
using Breeze.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace Breeze.AspNetCore {


  public class BreezeQueryFilterAttribute : ActionFilterAttribute {
    public override void OnActionExecuting(ActionExecutingContext context) {
      if (!context.ModelState.IsValid) {
        context.Result = new BadRequestObjectResult(context.ModelState);
      }
    }

    public override void OnActionExecuted(ActionExecutedContext context) {

      var qs = QueryFns.ExtractAndDecodeQueryString(context);
      var queryable = QueryFns.ExtractQueryable(context);

      if (!EntityQuery.NeedsExecution(qs, queryable)) {
        base.OnActionExecuted(context);
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
        var listResult = Enumerable.ToList((dynamic)queryable);
        listResult = EntityQuery.AfterExecution(eq, queryable, listResult);

        var qr = new QueryResult(listResult, inlineCount);
        context.Result = new ObjectResult(qr);
      }

      base.OnActionExecuted(context);

    }
  }

}

