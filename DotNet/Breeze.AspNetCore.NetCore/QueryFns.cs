using Breeze.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Breeze.AspNetCore {
  /// <summary> Static utility functions for processing queries </summary>
  public static class QueryFns {

    /// <summary> Get the IQueryable from the context.Result </summary>
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

    /// <summary> Get the query string from the HttpRequest </summary>
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
      if (q == "{}" || q == "") {
        return null;
      }
      return q;
    }

    /// <summary> Apply the Where, Order, Skip, and Take predicates from the request's query string to the IQueryable </summary>
    /// <remarks><example> Example: Apply query filtering, then aggregate the results.
    /// <code>
    /// // Apply EntityQuery to filter the IQueryable before aggregation
    /// var query = QueryFns.ApplyBreezeQuery(this.ControllerContext, dbContext.Orders);
    /// // Total the results
    /// var totals = query.Sum(o => o.TotalAmount).ToList();
    /// // Return an object, not an IEnumerable, else Breeze will attempt to apply the query again
    /// return new { total = totals[0] };
    /// </code></example></remarks>
    public static IQueryable<T> ApplyBreezeQuery<T>(ActionContext context, IQueryable<T> queryable) {
      // Build Breeze EntityQuery from string parameters
      var qs = QueryFns.ExtractAndDecodeQueryString(context);
      var eq = new EntityQuery(qs);
      eq.Validate(typeof(T));

      // Apply EntityQuery to filter the IQueryable before query execution
      queryable = eq.ApplyWhere(queryable, typeof(T)) as IQueryable<T>;
      queryable = EntityQuery.ApplyCustomLogic(eq, queryable, typeof(T)) as IQueryable<T>;
      queryable = eq.ApplyOrderBy(queryable, typeof(T)) as IQueryable<T>;
      queryable = eq.ApplySkip(queryable, typeof(T)) as IQueryable<T>;
      queryable = eq.ApplyTake(queryable, typeof(T)) as IQueryable<T>;
      return queryable;
    }

    /// <summary> Apply the Where, Order, Skip, and Take predicates from the request's query string to the IQueryable </summary>
    /// <remarks><example> Example: Apply query filtering, then aggregate the results.
    /// <code>
    /// // Apply EntityQuery from client to filter the IQueryable before aggregation
    /// var query = this.ApplyBreezeQuery(dbContext.Orders);
    /// // Total the results
    /// var totals = query.Sum(o => o.TotalAmount).ToList();
    /// // Return an object, not an IEnumerable, else Breeze will attempt to apply the query again
    /// return new { total = totals[0] };
    /// </code></example></remarks>
    public static IQueryable<T> ApplyBreezeQuery<T>(this ControllerBase controller, IQueryable<T> queryable) {
      return ApplyBreezeQuery(controller.ControllerContext, queryable);
    }

    /// <summary> Apply the Where predicate from the request's query string to the IQueryable </summary>
    /// <remarks><example> Example: Apply query Where clause, then aggregate the results.
    /// <code>
    /// // Apply Where clause from client to filter the IQueryable before aggregation
    /// var query = QueryFns.ApplyBreezeWhere(this.ControllerContext, dbContext.Orders);
    /// // Total the results
    /// var totals = query.Sum(o => o.TotalAmount).ToList();
    /// // Return an object, not an IEnumerable, else Breeze will attempt to apply the query again
    /// return new { total = totals[0] };
    /// </code></example></remarks>
    public static IQueryable<T> ApplyBreezeWhere<T>(ActionContext context, IQueryable<T> queryable) {
      // Build Breeze EntityQuery from string parameters
      var qs = QueryFns.ExtractAndDecodeQueryString(context);
      var eq = new EntityQuery(qs);
      eq.Validate(typeof(T));

      // Apply EntityQuery Where clause to filter the IQueryable before query execution
      queryable = eq.ApplyWhere(queryable, typeof(T)) as IQueryable<T>;
      return queryable;
    }

    /// <summary> Apply the Where predicate from the request's query string to the IQueryable </summary>
    /// <remarks><example> Example: Apply query filtering, then aggregate the results.
    /// <code>
    /// // Apply Where clause from client to filter the IQueryable before aggregation
    /// var query = this.ApplyBreezeWhere(dbContext.Orders);
    /// // Total the results
    /// var totals = query.Sum(o => o.TotalAmount).ToList();
    /// // Return an object, not an IEnumerable, else Breeze will attempt to apply the query again
    /// return new { total = totals[0] };
    /// </code></example></remarks>
    public static IQueryable<T> ApplyBreezeWhere<T>(this ControllerBase controller, IQueryable<T> queryable) {
      return ApplyBreezeWhere(controller.ControllerContext, queryable);
    }

  }
}
