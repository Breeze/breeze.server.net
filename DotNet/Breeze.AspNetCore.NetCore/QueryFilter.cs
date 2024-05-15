
using Breeze.AspNetCore.NetCore;
using Breeze.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Breeze.AspNetCore {

  /// <summary> Attribute to apply the request's query string to the returned IQueryable and execute it synchronously.</summary>
  /// <remarks> Put [BreezeQueryFilter] on a Controller class to apply Breeze query filtering
  /// and execution to each method that returns an IQueryable or IEnumerable.
  /// <para></para>
  /// You might want to use this class instead of [BreezeAsyncQueryFilter] because of https://github.com/dotnet/efcore/issues/18221
  /// <para></para>
  /// See <see href="https://breeze.github.io/doc-net/webapi-controller-core#breezequeryfilterattribute"/>
  /// </remarks>
  public class BreezeQueryFilterAttribute : ActionFilterAttribute {

    /// <summary> Check if context.ModelState is valid </summary>
    public override void OnActionExecuting(ActionExecutingContext context) {
      if (!context.ModelState.IsValid) {
        context.Result = new BadRequestObjectResult(context.ModelState);
      }
    }

    /// <summary> Extract the IQueryable from the context, apply the query, and execute it. </summary>
    public override void OnActionExecuted(ActionExecutedContext context) {

      // don't attempt to process queryable if we are throwing an error or cancelling
      if (context.Result is IStatusCodeActionResult scar && scar.StatusCode >= 400) {
        base.OnActionExecuted(context);
        return;
      }

      var qs = QueryFns.ExtractAndDecodeQueryString(context);
      var queryable = QueryFns.ExtractQueryable(context);

      if (!EntityQuery.NeedsExecution(qs, queryable)) {
        base.OnActionExecuted(context);
        return;
      }

      var eq = new EntityQuery(qs);
      var eleType = TypeFns.GetElementType(queryable.GetType());
      eq.Validate(eleType);

      var originalQueryable = queryable;
      queryable = eq.ApplyWhere(queryable, eleType);

      int? inlineCount = null;
      if (eq.IsInlineCountEnabled) {
        inlineCount = (int)Queryable.Count((dynamic)queryable);
      }

      if (context.HttpContext.RequestAborted.IsCancellationRequested) {
        context.Result = GetEmptyResult();
        base.OnActionExecuted(context);
        return;
      }

      queryable = EntityQuery.ApplyCustomLogic(eq, queryable, eleType);
      queryable = eq.ApplyOrderBy(queryable, eleType);
      queryable = eq.ApplySkip(queryable, eleType);
      queryable = eq.ApplyTake(queryable, eleType);
      queryable = eq.ApplySelect(queryable, eleType);
      queryable = EntityQuery.ApplyExpand(eq, queryable, eleType);

      if (queryable != originalQueryable) {
        // if a select or expand was encountered we need to
        // execute the DbQueries here, so that any exceptions thrown can be properly returned.
        // if we wait to have the query executed within the serializer, some exceptions will not
        // serialize properly.
        var listResult = Enumerable.ToList((dynamic)queryable);
        listResult = EntityQuery.AfterExecution(eq, queryable, listResult);

        var qr = new QueryResult(listResult, inlineCount);
        context.Result = new ObjectResult(qr);
      }

      base.OnActionExecuted(context);

    }

    /// <summary> Produce empty result for request cancellations </summary>
    private ObjectResult GetEmptyResult() {
      var emptyResult = new QueryResult(Enumerable.Empty<dynamic>(), null);
      return new ObjectResult(emptyResult) { StatusCode = 499 };
    }
  }


  /// <summary> Attribute to apply the request's query string to the returned IQueryable and execute it asynchronously. </summary>
  /// <remarks> Put [BreezeAsyncQueryFilter] on a Controller class to apply Breeze query filtering
  /// and execution to each method that returns an IQueryable or IEnumerable.
  /// <para></para>
  /// You might want to use this class instead of [BreezeQueryFilter] to get the benefits of async tasks and cancellable queries.
  /// <para></para>
  /// See <see href="https://breeze.github.io/doc-net/webapi-controller-core#breezequeryfilterattribute"/>
  /// </remarks>
  public class BreezeAsyncQueryFilterAttribute : ActionFilterAttribute, IAsyncActionFilter {
    /// <summary> If true, OperationCanceledExceptions will be caught and an empty result will be returned. </summary>
    public bool CatchCancellations { get; set; }
    /// <summary> Status code when cancellation results are returned; default is 499 Client Closed Connection </summary>
    public int CancellationStatusCode { get; set; } = 499;

    /// <summary> Extract the IQueryable from the context, apply the query, and execute it. </summary>
    override public async Task OnActionExecutionAsync(ActionExecutingContext executingContext, ActionExecutionDelegate next) {

      OnActionExecuting(executingContext);
      if (!executingContext.ModelState.IsValid) {
        executingContext.Result = new BadRequestObjectResult(executingContext.ModelState);
        return;
      }

      var executedContext = await next();

      // don't attempt to process queryable if we are throwing an error
      if (executedContext.Result is IStatusCodeActionResult scar && scar.StatusCode >= 400) {
        return;
      }

      var qs = QueryFns.ExtractAndDecodeQueryString(executedContext);
      var queryable = QueryFns.ExtractQueryable(executedContext);

      if (!EntityQuery.NeedsExecution(qs, queryable)) {
        base.OnActionExecuted(executedContext);
        return;
      }

      var eq = new EntityQuery(qs);
      var eleType = TypeFns.GetElementType(queryable.GetType());
      eq.Validate(eleType);

      var originalQueryable = queryable;
      queryable = eq.ApplyWhere(queryable, eleType);

      try {
        int? inlineCount = null;
        if (eq.IsInlineCountEnabled) {
          inlineCount = await queryable.Cast<dynamic>().CountAsync(executingContext.HttpContext.RequestAborted);
        }

        queryable = EntityQuery.ApplyCustomLogic(eq, queryable, eleType);
        queryable = eq.ApplyOrderBy(queryable, eleType);
        queryable = eq.ApplySkip(queryable, eleType);
        queryable = eq.ApplyTake(queryable, eleType);
        queryable = eq.ApplySelect(queryable, eleType);
        queryable = EntityQuery.ApplyExpand(eq, queryable, eleType);

        if (queryable != originalQueryable) {
          // if a select or expand was encountered we need to
          // execute the DbQueries here, so that any exceptions thrown can be properly returned.
          // if we wait to have the query executed within the serializer, some exceptions will not
          // serialize properly.
          var result = await queryable.Cast<dynamic>().ToListAsync(executingContext.HttpContext.RequestAborted);
          var listResult = EntityQuery.AfterExecution(eq, queryable, result);

          var qr = new QueryResult(listResult, inlineCount);
          executedContext.Result = new ObjectResult(qr);
        }

      } catch (OperationCanceledException) {
        if (CatchCancellations) {
          executedContext.Result = GetEmptyResult();
        } else {
          throw;
        }
      }

      OnActionExecuted(executedContext);
    }

    private ObjectResult GetEmptyResult() {
      var emptyResult = new QueryResult(Enumerable.Empty<dynamic>(), null);
      return new ObjectResult(emptyResult) { StatusCode = CancellationStatusCode };
    }

  }

}

