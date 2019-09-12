using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Breeze.AspNetCore {
  public static class QueryFns {

    public static IQueryable ExtractQueryable(ActionExecutedContext context) {
      var objResult = context.Result as ObjectResult;
      if (objResult == null) {
        return null;
      }

      var result = objResult.Value;
      IQueryable queryable = null;
      if (result is IQueryable) {
        queryable = (IQueryable)result;
      } else if (result is IEnumerable) {
        try {
          queryable = ((IEnumerable)result).AsQueryable();
        } catch {
          throw new Exception("Unable to convert this endpoints IEnumerable to an IQueryable. Try returning an IEnumerable<T> instead of just an IEnumerable.");
        }
      } else {
        throw new Exception("Unable to convert this endpoint to an IQueryable");
      }
      return queryable;
    }
    public static string ExtractAndDecodeQueryString(ActionContext context) {
      var qs = context.HttpContext.Request.QueryString;
      var q = WebUtility.UrlDecode(qs.Value);
      if (q.Length == 0) {
        return null;
      }
      var endIx = q.IndexOf('&');
      if (endIx > 1) {
        q = q.Substring(1, endIx - 1);
      } else {
        q = q.Substring(1);
      }
      if (q == "{}") {
        return null;
      }
      return q;
    }
  }
}
