using Breeze.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Linq;

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
    /// <summary> If true and request is POST, then attempt to extract the Breeze query from the request body. </summary>
    /// <remarks> There is some overhead in extracting the request body, so UsePost should only be used on endpoints that are expected to have queries.
    /// If the request body has already been read (for example, due to model binding), it will need to be rewound before the filter can read it. </remarks>
    public bool UsePost { get; set; }

    /// <summary> Sets max depth for Select and Expand clauses.
    /// Set to 0 to disallow Selects and Expands requested by the client.
    /// Set to null (the default) to allow unlimited depth.<br/>
    /// Returns 400 Bad Request if MaxDepth is violated. <br/>
    /// MaxDepth = 1 on IQueryable&lt;Customer&gt; will allow Expand = "Orders" but not "Orders.OrderDetails" <br/>
    /// MaxDepth = 0 on IQueryable&lt;Customer&gt; will allow Select = "Name" but not Select = "Orders" or Expand = "Orders"
    /// </summary>
    public int? MaxDepth { get; set; }

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

      // don't process queryable if flag has been set for this request
      if (QueryFns.IsSkipBreezeQueryFilter(context)) {
        base.OnActionExecuted(context);
        return;
      }

      var qs = QueryFns.ExtractAndDecodeQueryString(context, UsePost);
      var queryable = QueryFns.ExtractQueryable(context);

      if (!EntityQuery.NeedsExecution(qs, queryable)) {
        base.OnActionExecuted(context);
        return;
      }

      var eq = new EntityQuery(qs);
      var eleType = TypeFns.GetElementType(queryable.GetType());
      eq.Validate(eleType);

      var msg = CheckMaxDepth(eq, MaxDepth);
      if (msg != null) {
        context.Result = new BadRequestObjectResult(msg);
        return;
      }

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

    /// <summary> Check select and expand to see if MaxDepth is exceeded </summary>
    internal static string CheckMaxDepth(EntityQuery eq, int? maxDepth) {
      if (maxDepth != null && eq.SelectClause != null) {
        // check selects
        foreach (var sp in eq.SelectClause.Properties) {
          // okay to exceed the count by one, iff selecting a data property
          if (sp.Properties.Count > maxDepth + 1 || (sp.Properties.Count == maxDepth + 1 && !sp.IsDataProperty)) {
            return $"MaxDepth exceeded: {sp.PropertyPath}";
          }
        }
      }

      if (maxDepth != null && eq.ExpandClause != null) {
        // check expands
        foreach (var path in eq.ExpandClause.PropertyPaths) {
          var sp = path.Split('/', '.');
          // okay to exceed the count by one, iff selecting a data property
          if (sp.Length > maxDepth) {
            return $"MaxDepth exceeded: {path}";
          }
        }
      }

      return null;
    }
  }

}

