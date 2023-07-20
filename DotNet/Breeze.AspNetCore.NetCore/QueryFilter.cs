
using System;
using Breeze.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Linq;
using Breeze.AspNetCore.NetCore;
using System.Threading.Tasks;

namespace Breeze.AspNetCore {

  /// <summary> Attribute to apply the request's query string to the returned IQueryable </summary>
  /// <remarks> Put [BreezeQueryFilter] on a Controller class to apply Breeze query filtering
  /// and execution to each method that returns an IQueryable or IEnumerable.
  /// <para></para>
  /// See <see href="https://breeze.github.io/doc-net/webapi-controller-core#breezequeryfilterattribute"/>
  /// </remarks>
  public class BreezeQueryFilterAttribute : Attribute, IAsyncActionFilter {
    /// <summary>
    /// If true, OperationCanceledExceptions will be caught and an empty result will be returned.
    /// </summary>
    public bool CatchCancellations { get; set; }

    /// <summary> Extract the IQueryable from the context, apply the query, and execute it. </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext executingContext, ActionExecutionDelegate next) {
      var cancellationToken = executingContext.HttpContext.RequestAborted;

      if (!executingContext.ModelState.IsValid) {
        executingContext.Result = new BadRequestObjectResult(executingContext.ModelState);
      }

      var executedContext = await next();

      // don't attempt to process queryable if we are throwing an error
      if (executedContext.Result is IStatusCodeActionResult scar && scar.StatusCode >= 400) {
        return;
      }

      var qs = QueryFns.ExtractAndDecodeQueryString(executedContext);
      var queryable = QueryFns.ExtractQueryable(executedContext);

      if (!EntityQuery.NeedsExecution(qs, queryable)) {
        return;
      }

      var eq = new EntityQuery(qs);
      var eleType = TypeFns.GetElementType(queryable.GetType());
      eq.Validate(eleType);

      
      int? inlineCount = null;

      var originalQueryable = queryable;
      queryable = eq.ApplyWhere(queryable, eleType);
      
      if (eq.IsInlineCountEnabled) {
        inlineCount = (int)Queryable.Count((dynamic)queryable);
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
        if (CatchCancellations) {
          try {
            var result = await queryable.Cast<dynamic>().ToListAsync(cancellationToken);
            var listResult = EntityQuery.AfterExecution(eq, queryable, result);

            var qr = new QueryResult(listResult, inlineCount);
            executedContext.Result = new ObjectResult(qr);

          } catch (OperationCanceledException) {
            var emptyResult = new QueryResult(Enumerable.Empty<dynamic>(), null);
            executedContext.Result = new ObjectResult(emptyResult);
          }
        } else {
          var result = await queryable.Cast<dynamic>().ToListAsync(cancellationToken);
          var listResult = EntityQuery.AfterExecution(eq, queryable, result);

          var qr = new QueryResult(listResult, inlineCount);
          executedContext.Result = new ObjectResult(qr);
        }
      }
    }
  }

}

