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
      var result = (IQueryable) objResult.Value;

      if (eq.WherePredicate != null) {
        var func = QueryBuilder.BuildWhereFunc(eleType, eq.WherePredicate);
        result = func(result);
      }

      if (eq.OrderByClause != null) {
        var orderByItems = eq.OrderByClause.OrderByItems;
        var isThenBy = false;
        orderByItems.ToList().ForEach(obi => {
          var funcOb = QueryBuilder.BuildOrderByFunc(isThenBy, eleType, obi.PropertyPath);
          result = funcOb(result);
          isThenBy = true;
        });
      }

      if (eq.SelectClause != null) {
        var func = QueryBuilder.BuildSelectFunc(eleType, eq.SelectClause.PropertyPaths);
        result = func(result);
      }

      if (eq.ExpandClause != null) {
        eq.ExpandClause.PropertyPaths.ToList().ForEach(expand => {
          result = ((dynamic) result).Include(expand.Replace('/', '.'));
        });
        
      }

      if (objResult.Value != result) {
        context.Result = new ObjectResult(result);
      }

      // TODO:
      // if a select or expand was encountered we need to
      // execute the DbQueries here, so that any exceptions thrown can be properly returned.
      // if we wait to have the query executed within the serializer, some exceptions will not
      // serialize properly.

      base.OnActionExecuted(context);

    }
  }

  public class QueryResultFilterAttribute : ResultFilterAttribute {
    public override void OnResultExecuting(ResultExecutingContext context) {
      var y = context;
    }

    public override void OnResultExecuted(ResultExecutedContext context) {
      var x = context;
    }
  }
}

