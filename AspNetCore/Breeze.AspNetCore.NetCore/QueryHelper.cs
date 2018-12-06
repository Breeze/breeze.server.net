using Breeze.Core;
using Microsoft.AspNet.OData.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
//using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace Breeze.AspNetCore.NetCore
{
    public class QueryHelper
    {
        protected ODataQuerySettings querySettings;

        public QueryHelper(ODataQuerySettings querySettings)
        {
            this.querySettings = querySettings;
        }

        public QueryHelper(bool enableConstantParameterization, bool ensureStableOrdering, HandleNullPropagationOption handleNullPropagation, int? pageSize)
        {
            this.querySettings = NewODataQuerySettings(enableConstantParameterization, ensureStableOrdering, handleNullPropagation, pageSize);
        }

        public QueryHelper()
          : this(true, true, HandleNullPropagationOption.False, null)
        {
        }

        public static ODataQuerySettings NewODataQuerySettings(bool enableConstantParameterization, bool ensureStableOrdering, HandleNullPropagationOption handleNullPropagation, int? pageSize)
        {
            var settings = new ODataQuerySettings()
            {
                EnableConstantParameterization = enableConstantParameterization,
                EnsureStableOrdering = ensureStableOrdering,
                HandleNullPropagation = handleNullPropagation,
                PageSize = pageSize > 0 ? pageSize : null
            };
            return settings;
        }

        // Controls whether we always handle expands (vs. letting WebApi take care of it)
        public virtual bool ManuallyExpand { get { return false; } }

        /// <summary>
        /// Provide a hook to do any processing before applying the query.  This implementation does nothing.
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="queryOptions"></param>
        /// <returns></returns>
        public virtual IQueryable BeforeApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            return queryable;
        }


        public IQueryable ApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            return ApplyQuery(queryable, queryOptions, this.querySettings);
        }

        /// <summary>
        /// Apply the queryOptions to the query.  
        /// This method handles nested order-by statements the the current ASP.NET web api does not yet support.
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="queryOptions"></param>
        /// <param name="querySettings"></param>
        /// <returns></returns>
        public IQueryable ApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions, ODataQuerySettings querySettings)
        {

            // HACK: this is a hack because on a bug in querySettings.EnsureStableOrdering = true that overrides
            // any existing order by clauses, instead of appending to them.
            querySettings.EnsureStableOrdering = false;

            // Basic idea here is the current WebApi OData cannot support the following operations
            // 1) "orderby" with  nested properties
            // 2) "select" with complex types
            // 3) "selects" of "nested" properties unless accompanied by the appropriate "expand".  
            //     i.e. can't do Customers.select("orders") unless we also add expand("orders")

            // The workaround here is to bypass "select" and "orderBy" processing under these conditions 
            // This involves removing the "offending" queryOptions before asking the WebApi2 OData processing to do its thing
            // and then post processing the resulting IQueryable. 
            // We actually do this with all selects because it's easier than trying to determine if they are actually problematic. 

            // Another approach that DOESN'T work is to let WebApi2 OData try to do it stuff and then only handle the cases where it throws an exception.
            // This doesn't work because WebApi2 OData will actually just skip the portions of the query that it can't process and return what it can ( under some conditions). 

            var expandQueryString = queryOptions.RawValues.Expand;
            var orderByQueryString = queryOptions.RawValues.OrderBy;
            var selectQueryString = queryOptions.RawValues.Select;

            ODataQueryOptions newQueryOptions = queryOptions;
            if (!string.IsNullOrWhiteSpace(selectQueryString))
            {
                newQueryOptions = QueryHelper.RemoveSelectExpandOrderBy(newQueryOptions);
            }
            else if ((!string.IsNullOrWhiteSpace(orderByQueryString)) && orderByQueryString.IndexOf('/') >= 0)
            {
                newQueryOptions = QueryHelper.RemoveSelectExpandOrderBy(newQueryOptions);
            }
            else if (ManuallyExpand && !string.IsNullOrWhiteSpace(expandQueryString))
            {
                newQueryOptions = QueryHelper.RemoveSelectExpandOrderBy(newQueryOptions);
            }


            if (newQueryOptions == queryOptions)
            {
                return queryOptions.ApplyTo(queryable, querySettings);
            }
            else
            {
                // remove inlinecount or it will be executed two times
                if (newQueryOptions.Count != null)
                {
                    newQueryOptions = QueryHelper.RemoveInlineCount(newQueryOptions);
                }

                // apply default processing first with "unsupported" stuff removed. 
                var q = newQueryOptions.ApplyTo(queryable, querySettings);
                // then apply unsupported stuff. 

                string inlinecountString = queryOptions.RawValues.Count;
                if (!string.IsNullOrWhiteSpace(inlinecountString))
                {
                    if (inlinecountString == "allpages")
                    {
                        var inlineCount = (Int64)Queryable.Count((dynamic)q);
                        //queryOptions.Request.SetInlineCount(inlineCount);
                    }
                }

                q = ApplyOrderBy(q, queryOptions);
                var q2 = ApplySelect(q, queryOptions);
                if (q2 == q)
                {
                    q2 = ApplyExpand(q, queryOptions);
                }

                return q2;
            }


        }

        public static ODataQueryOptions RemoveSelectExpandOrderBy(ODataQueryOptions queryOptions)
        {
            var optionsToRemove = new List<String>() { "$select", "$expand", "$orderby", "$top", "$skip" };
            return RemoveOptions(queryOptions, optionsToRemove);
        }

        public static ODataQueryOptions RemoveInlineCount(ODataQueryOptions queryOptions)
        {
            var optionsToRemove = new List<String>() { "$inlinecount" };
            return RemoveOptions(queryOptions, optionsToRemove);
        }

        public static ODataQueryOptions RemoveOptions(ODataQueryOptions queryOptions, List<String> optionNames)
        {
            var request = queryOptions.Request;
            //var oldUri = new Uri(queryOptions.Request.QueryString.ToString());
            var oldUri = new Uri($"{request.Scheme}://{request.Host}{request.Path.ToString().TrimEnd('/')}/{request.QueryString}");

            var map = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(oldUri.Query).Where(d => (d.Key.Trim().Length > 0) && !optionNames.Contains(d.Key.Trim()))
                .Select(d => new KeyValuePair<string, string>(d.Key, d.Value));

            var qb = new Microsoft.AspNetCore.Http.Extensions.QueryBuilder(map);
            var newUrl = oldUri.Scheme + "://" + oldUri.Authority + oldUri.AbsolutePath.TrimEnd('/') + "/" + qb.ToQueryString();
            //var newUri = new Uri(newUrl);

            //request.Path = new PathString(newUrl);
            request.QueryString = qb.ToQueryString();
            var newQo = new ODataQueryOptions(queryOptions.Context, request);
            return newQo;
            //return queryOptions;
        }


        /// <summary>
        /// Apply the select clause to the queryable
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="selectQueryString"></param>
        /// <returns></returns>
        public virtual IQueryable ApplySelect(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            var selectQueryString = queryOptions.RawValues.Select;
            if (string.IsNullOrEmpty(selectQueryString)) return queryable;
            var selectClauses = selectQueryString.Split(',').Select(sc => sc.Replace('/', '.')).ToList();
            var elementType = TypeFns.GetElementType(queryable.GetType());
            var func = QueryBuilder.BuildSelectFunc(elementType, selectClauses);
            return func(queryable);
        }

        /// <summary>
        /// Apply to expands clause to the queryable
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="expandsQueryString"></param>
        /// <returns></returns>
        public virtual IQueryable ApplyExpand(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            var expandQueryString = queryOptions.RawValues.Expand;
            if (string.IsNullOrEmpty(expandQueryString)) return queryable;
            var eleType = TypeFns.GetElementType(queryable.GetType());
            expandQueryString.Split(',').Select(s => s.Trim()).ToList().ForEach(expand => {
                //queryable = ((dynamic)queryable).Include(expand.Replace('/', '.'));
                var method = TypeFns.GetMethodByExample((IQueryable<String> q) => EntityFrameworkQueryableExtensions.Include<String>(q, "dummyPath"), eleType);
                var func = QueryBuilder.BuildIQueryableFunc(eleType, method, expand);
                queryable = func(queryable);
            });
            return queryable;
        }


        public virtual IQueryable ApplyOrderBy(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            var elementType = TypeFns.GetElementType(queryable.GetType());
            var result = queryable;

            var orderByString = queryOptions.RawValues.OrderBy;
            if (!string.IsNullOrEmpty(orderByString))
            {
                var orderByClauses = orderByString.Split(',').ToList();
                var isThenBy = false;
                orderByClauses.ForEach(obc => {
                    var parts = obc.Trim().Replace("  ", " ").Split(' ');
                    var propertyPath = parts[0];
                    bool isDesc = parts.Length > 1 && parts[1] == "desc";
                    var odi = new OrderByClause.OrderByItem(parts[0], isDesc);

                    var func = QueryBuilder.BuildOrderByFunc(isThenBy, elementType, odi);
                    result = func(result);
                    isThenBy = true;
                });
            }

            var skipQueryString = queryOptions.RawValues.Skip;
            if (!string.IsNullOrWhiteSpace(skipQueryString))
            {
                var count = int.Parse(skipQueryString);
                var method = TypeFns.GetMethodByExample((IQueryable<String> q) => Queryable.Skip<String>(q, 999), elementType);
                var func = BuildIQueryableFunc(elementType, method, count);
                result = func(result);
            }

            var topQueryString = queryOptions.RawValues.Top;
            if (!string.IsNullOrWhiteSpace(topQueryString))
            {
                var count = int.Parse(topQueryString);
                var method = TypeFns.GetMethodByExample((IQueryable<String> q) => Queryable.Take<String>(q, 999), elementType);
                var func = BuildIQueryableFunc(elementType, method, count);
                result = func(result);
            }

            return result;
        }

        private static Func<IQueryable, IQueryable> BuildIQueryableFunc<TArg>(Type instanceType, MethodInfo method, TArg parameter, Type queryableBaseType = null)
        {
            if (queryableBaseType == null)
            {
                queryableBaseType = typeof(IQueryable<>);
            }
            var paramExpr = Expression.Parameter(typeof(IQueryable));
            var queryableType = queryableBaseType.MakeGenericType(instanceType);
            var castParamExpr = Expression.Convert(paramExpr, queryableType);

            var callExpr = Expression.Call(method, castParamExpr, Expression.Constant(parameter));
            var castResultExpr = Expression.Convert(callExpr, typeof(IQueryable));
            var lambda = Expression.Lambda(castResultExpr, paramExpr);
            var func = (Func<IQueryable, IQueryable>)lambda.Compile();
            return func;
        }




        /// <summary>
        /// Perform any work after the query is executed.  Does nothing in this implementation but is available to derived classes.
        /// </summary>
        /// <param name="queryResult"></param>
        /// <returns></returns>
        public virtual IEnumerable PostExecuteQuery(IEnumerable queryResult)
        {
            return queryResult;
        }

        /// <summary>
        /// Replaces the response.Content with the query results, wrapped in a QueryResult object if necessary.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="responseObject"></param>
        /// <param name="queryable"></param>
        public virtual void WrapResult(HttpRequest request, ActionExecutedContext actionExecutedContext, IQueryable queryResult)
        {
            Object tmp;
            request.HttpContext.Items.TryGetValue("MS_InlineCount", out tmp);
            var inlineCount = (Int64?)tmp;

            // if a select or expand was encountered we need to
            // execute the DbQueries here, so that any exceptions thrown can be properly returned.
            // if we wait to have the query executed within the serializer, some exceptions will not
            // serialize properly.

            var listQueryResult = Enumerable.ToList((dynamic)queryResult);

            var elementType = queryResult.ElementType;
            if (elementType.Name.StartsWith("SelectAllAndExpand"))
            {
                var prop = elementType.GetProperties().FirstOrDefault(pi => pi.Name == "Instance");
                var mi = prop.GetGetMethod();
                var lqr = (List<Object>)listQueryResult;
                listQueryResult = (dynamic)lqr.Select(item => {
                    var instance = mi.Invoke(item, null);
                    return (Object)instance;
                }).ToList();
            }

            // HierarchyNodeExpressionVisitor
            listQueryResult = PostExecuteQuery((IEnumerable)listQueryResult);

            if (listQueryResult != null || inlineCount.HasValue)
            {
                Object result = listQueryResult;
                if (inlineCount.HasValue)
                {
                    result = new QueryResult() { Results = listQueryResult, InlineCount = inlineCount };
                }
                var objResult = actionExecutedContext.Result as ObjectResult;
                if (objResult != null && objResult.Value != null)
                {
                    var resultType = objResult.Value.GetType();
                    if (resultType.Name != "String")
                    {
                        var ss = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                        ss = JsonSerializationFns.UpdateWithDefaults(ss);
                        //ss.TypeNameHandling = TypeNameHandling.None;
                        //ss.PreserveReferencesHandling = PreserveReferencesHandling.None;
                        //var finalJsonIndented = JsonConvert.SerializeObject(result, Formatting.Indented, ss);
                        var jsonResult = new JsonResult(result, ss);
                        actionExecutedContext.Result = jsonResult;
                        //var oc = new ObjectContent(result.GetType(), result, formatter);
                        //var oc = new ObjectResult(result.GetType(), result);
                        //response.Content = oc;
                        //actionExecutedContext.Result = 
                        //actionExecutedContext.HttpContext.Response.con
                    }
                }
            }
        }


        /// <summary>
        /// Configure the JsonFormatter.  Does nothing in this implementation but is available to derived classes.
        /// </summary>
        /// <param name="request">Used to retrieve the current JsonFormatter</param>
        /// <param name="queryable">Used to obtain the ISession</param>
        public virtual void ConfigureFormatter(ActionExecutedContext actionExecutedContext, IQueryable queryable)
        {
            var controller = actionExecutedContext.Controller as Microsoft.AspNetCore.Mvc.Controller;
            var formatSelector = actionExecutedContext.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.OutputFormatterSelector>();
            
            //var jsonFormatter = request.GetConfiguration().Formatters.JsonFormatter;
            //ConfigureFormatter(jsonFormatter, queryable);
        }

        /// <summary>
        /// Configure the JsonFormatter.  Does nothing in this implementation but is available to derived classes.
        /// </summary>
        /// <param name="jsonFormatter"></param>
        /// <param name="queryable">Used to obtain the ISession</param>
        //public virtual void ConfigureFormatter(JsonMediaTypeFormatter jsonFormatter, IQueryable queryable)
        //{
        //}

        /// <summary>
        /// Release any resources associated with this QueryHelper.
        /// </summary>
        /// <param name="responseObject">Response payload, which may have associated resources.</param>
        public virtual void Close(object responseObject)
        {
        }

    }

    /// <summary>
    /// Wrapper for results that have an InlineCount, to support paged result sets.
    /// </summary>
    public class QueryResult
    {
        public dynamic Results { get; set; }
        public Int64? InlineCount { get; set; }
    }
}
