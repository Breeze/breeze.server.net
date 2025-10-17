using Breeze.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Models.NorthwindIB.CF;

namespace TestBreeze {
  public class Util {
    /// <summary>
    /// Create a new NorthwindIBContext_CF DContext using a hard-coded connection string
    /// </summary>
    public static NorthwindIBContext_CF NorthwindIB() {
      var nwcf = "Data Source=.;Initial Catalog=NorthwindIB;Integrated Security=True;Encrypt=False;MultipleActiveResultSets=True";
      var options = new DbContextOptionsBuilder<NorthwindIBContext_CF>().UseSqlServer(nwcf, o => o.UseCompatibilityLevel(120)).Options;
      var ctx = new NorthwindIBContext_CF(options);
      return ctx;
    }


    /// <summary>
    /// Initialize an ActionExecutedContext for testing ActionFilters
    /// </summary>
    /// <param name="queryString">Request query string, starting with "?".  For breeze, it should be like "?{id:3}"</param>
    /// <param name="queryable">IQueryable of entities, as would be the result of the controller method</param>
    /// <returns></returns>
    public static ActionExecutedContext NewActionExecutedContext(string queryString, IQueryable queryable) {
      var modelState = new ModelStateDictionary();
      //modelState.AddModelError("", "error");
      var httpContext = new DefaultHttpContext {
        Request = {
          QueryString = new QueryString(queryString)
        }
      };
      var aeContext = new ActionExecutedContext(
        new ActionContext(
          httpContext: httpContext,
          routeData: new RouteData(),
          actionDescriptor: new ActionDescriptor(),
          modelState: modelState
        ),
        new List<IFilterMetadata>(),
        controller: new()
      );

      aeContext.Result = new ObjectResult(queryable);
      return aeContext;
    }

    /// <summary>
    /// Initialize an ActionExecutingContext for testing IAsyncActionFilters
    /// </summary>
    /// <param name="queryString">Request query string, starting with "?".  For breeze, it should be like "?{id:3}"</param>
    /// <param name="queryable">IQueryable of entities, as would be the result of the controller method</param>
    /// <returns></returns>
    //public static ActionExecutingContext NewActionExecutingContext(string queryString, IQueryable queryable) {
    //  var modelState = new ModelStateDictionary();
    //  var httpContext = new DefaultHttpContext {
    //    Request = {
    //      QueryString = new QueryString(queryString)
    //    }
    //  };
    //  var aeContext = new ActionExecutingContext(
    //    new ActionContext(
    //      httpContext: httpContext,
    //      routeData: new RouteData(),
    //      actionDescriptor: new ActionDescriptor(),
    //      modelState: modelState
    //    ),
    //    new List<IFilterMetadata>(),
    //    new Dictionary<string, object?>(),
    //    controller: new()
    //  );

    //  aeContext.Result = new ObjectResult(queryable);
    //  return aeContext;
    //}

    /// <summary> Initialize an empty ActionExecutingContext for testing IAsyncActionFilters </summary>
    public static ActionExecutingContext NewActionExecutingContext() {
      var aeContext = new ActionExecutingContext(
        new ActionContext(
          httpContext: new DefaultHttpContext(),
          routeData: new RouteData(),
          actionDescriptor: new ActionDescriptor(),
          modelState: new ModelStateDictionary()
        ),
        new List<IFilterMetadata>(),
        new Dictionary<string, object?>(),
        controller: new()
      );
      return aeContext;
    }

    /// <summary> Get the delegate to use as the "next" parameter in OnActionExecutionAsync.
    /// The delegate creates a ActionExecutedContext containing the queryString and queryable.</summary>
    //public static ActionExecutionDelegate GetNextDelegate(string queryString, IQueryable queryable) {
    //  var next = new ActionExecutionDelegate(() => Task.FromResult(NewActionExecutedContext(queryString, queryable)));
    //  return next;
    //}

    /// <summary> Get the delegate to use as the "next" parameter in OnActionExecutionAsync. </summary>
    public static ActionExecutionDelegate GetNextDelegate(ActionExecutedContext aec) {
      var next = new ActionExecutionDelegate(() => Task.FromResult(aec));
      return next;
    }

    /// <summary>
    /// Assert that actionResult contains a QueryResult containing an IEnumerable of T, and return it as a list.
    /// </summary>
    /// <typeparam name="T">Entity type (e.g. Customer), or object for projections</typeparam>
    /// <param name="actionResult"></param>
    /// <returns>The list extracted from the result </returns>
    public static List<T> AssertListResult<T>(IActionResult? actionResult) {
      Assert.IsInstanceOfType<ObjectResult>(actionResult);
      var result = ((ObjectResult)actionResult).Value;
      Assert.IsInstanceOfType<QueryResult>(result);
      var rows = ((QueryResult)result).Results.Cast<T>().ToList();
      return rows;
    }

    /// <summary>
    /// Assert that actionResult is a BadRequestObjectResult containing a string with the given prefix.
    /// </summary>
    /// <param name="actionResult"></param>
    /// <param name="prefix"></param>
    public static void AssertBadRequest(IActionResult? actionResult, string prefix) {
      Assert.IsInstanceOfType<BadRequestObjectResult>(actionResult);
      var result = ((ObjectResult)actionResult).Value;
      Assert.IsInstanceOfType<string>(result);
      Assert.IsTrue(((string)result).StartsWith(prefix));
    }

  }
}
