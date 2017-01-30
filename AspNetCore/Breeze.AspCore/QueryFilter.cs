using Foo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Breeze.Query;
using Breeze.ContextProvider;

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

      var qs = context.HttpContext.Request.QueryString;
      var q = WebUtility.UrlDecode(qs.Value);
      if (q.Length == 0) {
        base.OnActionExecuted(context);
        return;
      }
      q = q.Substring(1, q.Length - 2);
      
      
      var eq = new EntityQuery(q);
      var eleType = TypeFns.GetElementType(objResult.Value.GetType());
      eq.Validate(eleType);
      // TODO: handle IEnumerable as well.
      var result = (IQueryable) objResult.Value;
      int? inlineCount = null;

      if (eq.WherePredicate != null) {
        result = QueryBuilder.ApplyWhere(result, eleType, eq.WherePredicate);
      }

      if (eq.IsInlineCountEnabled) {
        inlineCount = (int)Queryable.Count((dynamic)result);
      }

      if (eq.OrderByClause != null) {
        result = QueryBuilder.ApplyOrderBy(result, eleType, eq.OrderByClause);
      }

      if (eq.SkipCount.HasValue) {
        result = QueryBuilder.ApplySkip(result, eleType, eq.SkipCount.Value);
      }

      if (eq.TakeCount.HasValue) {
        result = QueryBuilder.ApplyTake(result, eleType, eq.TakeCount.Value);
      }

      if (eq.SelectClause != null) {
        result = QueryBuilder.ApplySelect(result, eleType, eq.SelectClause);
      }

      if (eq.ExpandClause != null) {
        eq.ExpandClause.PropertyPaths.ToList().ForEach(expand => {
          result = ((dynamic) result).Include(expand.Replace('/', '.'));
        });
      }

      if (objResult.Value != result) {
        // if a select or expand was encountered we need to
        // execute the DbQueries here, so that any exceptions thrown can be properly returned.
        // if we wait to have the query executed within the serializer, some exceptions will not
        // serialize properly.
        var listResult = Enumerable.ToList((dynamic)result);
        var qr = new QueryResult(listResult, inlineCount);
        context.Result = new ObjectResult(qr);
      }
      

      base.OnActionExecuted(context);

    }
  }
  
  

  //public class CustomExceptionFilterAttribute : ExceptionFilterAttribute {
  //  public override void OnException(ExceptionContext context) {
  //    var exception = context.Exception;
  //    context.Result = new JsonResult(exception.Message);
      
  //  }
  //}
}

