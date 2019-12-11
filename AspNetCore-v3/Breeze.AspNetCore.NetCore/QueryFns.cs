using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Breeze.AspNetCore {
  public static class QueryFns {

    public static IQueryable ExtractQueryable(ActionExecutedContext context, bool throwOnError = false) {
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
          if (throwOnError) throw new Exception("Unable to convert this endpoints IEnumerable to an IQueryable. Try returning an IEnumerable<T> instead of just an IEnumerable.");
        }
      } else {
        if (throwOnError) throw new Exception("Unable to convert this endpoint to an IQueryable");
      }
      return queryable;
    }
    public static string ExtractAndDecodeQueryString(ActionContext context) {
      var qs = context.HttpContext.Request.QueryString;
      var q = WebUtility.UrlDecode(qs.Value);
      if (q.Length == 0) {
        return null;
      }
      // escape all & within quotes
      var marker = "~`~";
      // Next line not needed because quotes are always converted to double quotes on the server 
      // q = Regex.Replace(q, @"&(?=[^']*'([^']*'[^']*')*[^']*$)", marker);
      q = Regex.Replace(q, @"&(?=[^\""]*""([^""]*""[^""]*"")*[^""]*$)", marker);
      var endIx = q.IndexOf('&');
      if (endIx > 1) {
        q = q.Substring(1, endIx - 1);
      } else {
        q = q.Substring(1);
      }
      q = q.Replace(marker, "&");
      if (q == "{}") {
        return null;
      }
      return q;
    }
  }
}
