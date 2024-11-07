using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Text;
using Breeze.AspNetCore.NetCore;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Breeze.Persistence.EFCore
{
    /// <summary>
    /// Extend Web API's <see cref="EnableQueryAttribute"/>
    /// for expected Breeze OData-like query support.
    /// </summary>
    /// <remarks>
    /// Remember to add it to the Filters for your configuration.
    /// Automatically added to each IQueryable method when
    /// you put the [BreezeController] attribute on an ApiController class.
    /// <para>
    /// See also http://blogs.msdn.com/b/webdev/archive/2014/03/13/getting-started-with-asp-net-web-api-2-2-for-odata-v4-0.aspx
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class EnableBreezeQueryAttribute : EnableQueryAttribute
    {

        private static string QUERY_HELPER_KEY = "EnableBreezeQueryAttribute_QUERY_HELPER_KEY";

        public EnableBreezeQueryAttribute()
        {
            // ensure EnableQueryAttribute supports Expand and Select by default because Breeze does.
            // Todo: confirm that this is still necessary as it was for predecessor QueryableAttribute
            this.AllowedQueryOptions = AllowedQueryOptions.Supported | AllowedQueryOptions.Expand | AllowedQueryOptions.Select;
        }

        private Uri RequestUri = null;

        /// <summary>
        /// Get the QueryHelper instance for the current request.  We use a single instance per request because
        /// QueryHelper is stateful, and may preserve state between the ApplyQuery and OnActionExecuted methods.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected QueryHelper GetQueryHelper(HttpRequest request)
        {
            this.RequestUri = new Uri(request.Scheme + "://" + request.Host + (request.Path.HasValue ? request.Path.Value : String.Empty) + (request.QueryString.HasValue ? request.QueryString.Value : String.Empty));
            object qh;
            if (!request.HttpContext.Items.TryGetValue(QUERY_HELPER_KEY, out qh))
            {
                qh = NewQueryHelper();
                request.HttpContext.Items.Add(QUERY_HELPER_KEY, qh);
            }
            return (QueryHelper)qh;
        }

        protected virtual QueryHelper NewQueryHelper()
        {
            return new QueryHelper(GetODataQuerySettings());
        }

        public ODataQuerySettings GetODataQuerySettings()
        {
            var settings = new ODataQuerySettings
            {
                EnableConstantParameterization = this.EnableConstantParameterization,
                EnsureStableOrdering = this.EnsureStableOrdering,
                HandleNullPropagation = this.HandleNullPropagation,
                PageSize = this.PageSize > 0 ? this.PageSize : (int?)null
            };
            return settings;
        }

        /// <summary>
        /// Called when the action is executed.  If the return type is IEnumerable or IQueryable,
        /// calls OnActionExecuted in the base class, which in turn calls ApplyQuery.
        /// </summary>
        /// <param name="actionExecutedContext">The action executed context.</param>
        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            var response = actionExecutedContext.HttpContext.Response;
            if (response == null)
            {
                return;
            }
            object responseObject = (actionExecutedContext.Result as ObjectResult)?.Value;
            if (responseObject == null)
            {
                return;
            }

            var request = actionExecutedContext.HttpContext.Request;
            var returnType = (actionExecutedContext.Result as ObjectResult).Value?.GetType();
            var queryHelper = GetQueryHelper(request);

            try
            {
                if (!response.IsSuccessStatusCode())
                {
                    return;
                }
                if (typeof(IEnumerable).IsAssignableFrom(returnType) || responseObject is IEnumerable)
                {
                    // We think that EnableQueryAttribute only applies for IQueryable and IEnumerable return types
                    // Our version calls ValidateQuery and then ApplyQuery
                    // Todo: confirm that this is still necessary as it was for predecessor QueryableAttribute
                    base.OnActionExecuted(actionExecutedContext);
                    if (!actionExecutedContext.HttpContext.Response.IsSuccessStatusCode())
                    {
                        return;
                    }
                    responseObject = (actionExecutedContext.Result as ObjectResult)?.Value;
                    if (responseObject == null)
                    {
                        return;
                    }
                    var queryResult = responseObject as IQueryable;
                    if (queryResult == null && responseObject is IEnumerable)
                        queryResult = (responseObject as IEnumerable).AsQueryable();
                    queryHelper.WrapResult(request, actionExecutedContext, queryResult);
                }
                // For non-IEnumerable results, post-processing must be done manually by the developer.
                queryHelper.ConfigureFormatter(actionExecutedContext, responseObject as IQueryable);
            }
            finally
            {
                queryHelper.Close(responseObject);
            }
        }

        public override void ValidateQuery(HttpRequest request, ODataQueryOptions queryOptions)
        {
            try
            {
                base.ValidateQuery(request, queryOptions);
            }
            catch (Exception e)
            {
                // Ignore error if its message is like "Only properties specified in $expand can be traversed in $select query options"
                // because Breeze CAN support this by bypassing the OData processing.
                if (!(e.Message.Contains("$expand") && e.Message.Contains("$select")))
                {
                    //if (e.Message.Contains("$orderby") && e.Message.Contains("TableName"))
                    //    return;
                    //throw; // any other error
                }
            }
        }

        /// <summary>
        /// All standard OData web api support is handled here (except select and expand).
        /// This method also handles nested orderby statements the the current ASP.NET web api does not yet support.
        /// This method is called by base.OnActionExecuted
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="queryOptions"></param>
        /// <returns></returns>
        public override IQueryable ApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions)
        {

            var queryHelper = GetQueryHelper(queryOptions.Request);

            queryable = queryHelper.BeforeApplyQuery(queryable, queryOptions);
            queryable = queryHelper.ApplyQuery(queryable, queryOptions);
            return queryable;
        }

    }

}
