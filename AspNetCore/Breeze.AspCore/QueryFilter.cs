using Breeze.ContextProvider;
using Breeze.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace Breeze.AspCore {


  public class QueryFilterAttribute : ActionFilterAttribute {
    public override void OnActionExecuting(ActionExecutingContext context) {
      if (!context.ModelState.IsValid) {
        context.Result = new BadRequestObjectResult(context.ModelState);
      }
    }

    public override void OnActionExecuted(ActionExecutedContext context) {
      var objResult = context.Result as ObjectResult;
      if (objResult == null) {
        base.OnActionExecuted(context);
        return;
      }

      var result = objResult.Value;

      var qs = context.HttpContext.Request.QueryString;
      var q = WebUtility.UrlDecode(qs.Value);
      if (q.Length == 0) {
        base.OnActionExecuted(context);
        return;
      }
      var endIx = q.IndexOf('&');
      if (endIx > 1) { 
        q = q.Substring(1, endIx - 1);
      } else {
        q = q.Substring(1);
      }
      if (q == "{}") {
        base.OnActionExecuted(context);
        return;
      }

      var eq = new EntityQuery(q);
      var eleType = TypeFns.GetElementType(result.GetType());
      eq.Validate(eleType);

      IQueryable queryableResult = null;
      if (result is IQueryable) {
        queryableResult = (IQueryable)result;
      } else if (result is IEnumerable) {
        try {
          queryableResult = ((IEnumerable)result).AsQueryable();
        } catch {
          throw new Exception("Unable to convert this endpoints IEnumerable to an IQueryable. Try returning an IEnumerable<T> instead of just an IEnumerable.");
        }
      } else {
        throw new Exception("Unable to convert this endpoint to an IQueryable");
      }
      
      int? inlineCount = null;

      if (eq.WherePredicate != null) {
        queryableResult = QueryBuilder.ApplyWhere(queryableResult, eleType, eq.WherePredicate);
      }

      if (eq.IsInlineCountEnabled) {
        inlineCount = (int)Queryable.Count((dynamic)queryableResult);
      }

      if (eq.OrderByClause != null) {
        queryableResult = QueryBuilder.ApplyOrderBy(queryableResult, eleType, eq.OrderByClause);
      }

      if (eq.SkipCount.HasValue) {
        queryableResult = QueryBuilder.ApplySkip(queryableResult, eleType, eq.SkipCount.Value);
      }

      if (eq.TakeCount.HasValue) {
        queryableResult = QueryBuilder.ApplyTake(queryableResult, eleType, eq.TakeCount.Value);
      }

      if (eq.SelectClause != null) {
        queryableResult = QueryBuilder.ApplySelect(queryableResult, eleType, eq.SelectClause);
      }

      if (eq.ExpandClause != null) {
        eq.ExpandClause.PropertyPaths.ToList().ForEach(expand => {
          queryableResult = ((dynamic) queryableResult).Include(expand.Replace('/', '.'));
        });
      }

      if (result != queryableResult) {
        // if a select or expand was encountered we need to
        // execute the DbQueries here, so that any exceptions thrown can be properly returned.
        // if we wait to have the query executed within the serializer, some exceptions will not
        // serialize properly.
        var listResult = Enumerable.ToList((dynamic)queryableResult);
        var qr = new QueryResult(listResult, inlineCount);
        context.Result = new ObjectResult(qr);
      }
      

      base.OnActionExecuted(context);

    }
  }
  


}

