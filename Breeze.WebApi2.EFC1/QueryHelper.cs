using Breeze.ContextProvider;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqKit;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Web.Http.OData.Query;
using OData.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading.Tasks;
using System.Threading;

namespace Breeze.WebApi2.EFC1
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

        private MethodInfo _LongCountAsynchMI = null;
        private Task<Int64> _inlineCountT = null;

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
			var filterQueryString = queryOptions.RawValues.Filter;

			ODataQueryOptions newQueryOptions = queryOptions;
			if (!string.IsNullOrWhiteSpace(selectQueryString))
			{
				newQueryOptions = QueryHelper.RemoveSelectExpandOrderBy(newQueryOptions);
			}
			else if ((!string.IsNullOrWhiteSpace(orderByQueryString)) && orderByQueryString.IndexOf('/') >= 0)
			{
				newQueryOptions = QueryHelper.RemoveSelectExpandOrderBy(newQueryOptions);
			}
			//else if (ManuallyExpand && !string.IsNullOrWhiteSpace(expandQueryString))
			else if (!string.IsNullOrWhiteSpace(expandQueryString))
			{
				newQueryOptions = QueryHelper.RemoveSelectExpandOrderBy(newQueryOptions);
			}
			else if (!string.IsNullOrWhiteSpace(filterQueryString))
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
                if (newQueryOptions.InlineCount != null)
                {
                    newQueryOptions = QueryHelper.RemoveInlineCount(newQueryOptions);
                }

                // apply default processing first with "unsupported" stuff removed. 
                var q = newQueryOptions.ApplyTo(queryable, querySettings);
                // then apply unsupported stuff. 

                // remove any ordering
                q = RemoveOrderBy(q);

                // apply any filtering
                q = ApplyFilter(q, queryOptions);

                string inlinecountString = queryOptions.RawValues.InlineCount;
                if (!string.IsNullOrWhiteSpace(inlinecountString))
                {
                    if (inlinecountString == "allpages")
                    {
                        // TODO: make this asynch
                        //var inlineCount = (Int64)Queryable.LongCount((dynamic)q);
                        //queryOptions.Request.SetInlineCount(inlineCount);

                        // create new query for count
                        var countQ = q.Provider.CreateQuery(q.Expression) as IQueryable;
                        if (_LongCountAsynchMI == null)
                        {
                            _LongCountAsynchMI = typeof(EntityFrameworkQueryableExtensions).GetMethods().Where(me => me.Name == "LongCountAsync" && me.GetParameters().Count() == 2).FirstOrDefault();
                        }
                        var countQEntityType = TypeFns.GetElementType(q.Expression.Type);
                        var methodMI = _LongCountAsynchMI.MakeGenericMethod(countQEntityType);

                        try
                        {
                            this._inlineCountT = methodMI.Invoke(countQ, new Object[] { countQ, new System.Threading.CancellationToken() }) as Task<long>;
                        }
                        catch (Exception ex)
                        {

                        }



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
			var optionsToRemove = new List<String>() { "$select", "$expand", "$orderby", "$top", "$skip", "$filter" };
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
			var oldUri = request.RequestUri;

			var map = oldUri.ParseQueryString();
			var newQuery = map.Keys.Cast<String>()
							  .Where(k => (k.Trim().Length > 0) && !optionNames.Contains(k.Trim()))
							  .Select(k => k + "=" + map[k])
							  .ToAggregateString("&");

			var newUrl = oldUri.Scheme + "://" + oldUri.Authority + oldUri.AbsolutePath + "?" + newQuery;
			var newUri = new Uri(newUrl);

			var newRequest = new HttpRequestMessage(request.Method, newUri);
			var newQo = new ODataQueryOptions(queryOptions.Context, newRequest);
			return newQo;
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
			var result = queryable;

			var expandQueryString = queryOptions.RawValues.Expand;
			if (string.IsNullOrEmpty(expandQueryString)) return queryable;

            var expandQueryClauses = expandQueryString.Split(',').Select(s => s.Trim()).ToList();
			expandQueryClauses.ForEach(expand => {
                result = IncludeByString(result, expand);
			});
			return result;
		}

        public IQueryable IncludeByString(IQueryable queryable, String expandQueryString)
        {
            var result = queryable.Expression;
            if (string.IsNullOrEmpty(expandQueryString)) return queryable;

            var baseElementType = TypeFns.GetElementType(queryable.GetType());

            expandQueryString = expandQueryString.Replace('/', '.');
            var expandEntities = expandQueryString.Split('.').ToList();
            var isThenInclude = false;
            expandEntities.ForEach(propertyName =>
            {
                var parmElementType = isThenInclude ? GetIncludeableElementType(result.Type) : TypeFns.GetElementType(result.Type);

                var propertyInfo = parmElementType.GetProperty(propertyName);
                if (propertyInfo == null)
                {
                    throw new Exception("Unable to locate property: " + propertyName + " on type: " + parmElementType.ToString());
                }

                if (isThenInclude)
                {
                    result = ThenIncludeString(result, propertyName, parmElementType);
                }
                else
                {
                    result = IncludeString(result, propertyName);
                }


                isThenInclude = true;
            });
            return queryable.Provider.CreateQuery(result) as IQueryable;
        }

        private static Type GetIncludeableElementType(Type type)
        {
            Type result = null;
            if (type.Name.StartsWith("IIncludableQueryable"))
            {
                result = type.GetGenericArguments()[1];
                if (result.IsGenericType && result.Name.StartsWith("ICollection") && result.GetGenericArguments().Length > 0)
                {
                    result = result.GetGenericArguments()[0];
                }
            }
            return result;
        }

        private static LambdaExpression GeneratePropertySelector(Type entityType, String propertyName, out Type resultType)
        {
            // Create a parameter to pass into the Lambda expression (Entity => Entity.OrderByField).
            var parameter = Expression.Parameter(entityType, "o");
            //  create the selector part
            PropertyInfo property = entityType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Expression propertyAccess = Expression.MakeMemberAccess(parameter, property);
            resultType = property.PropertyType;
            // Create the order by expression.
            return Expression.Lambda(propertyAccess, parameter);
        }
        private static MethodCallExpression GenerateMethodCall(Expression queryableExpr, string methodName, String propertyName, Type forceEntityType = null)
        {
            var baseEntityType = TypeFns.GetElementType(queryableExpr.Type);
            var entityType = forceEntityType != null ? forceEntityType : baseEntityType;
            Type selectorResultType;
            LambdaExpression selector = GeneratePropertySelector(entityType, propertyName, out selectorResultType);
            var parmTypes = new List<Type> { baseEntityType };
            if (forceEntityType != null)
                parmTypes.Add(forceEntityType);
            parmTypes.Add(selectorResultType);
            var methodMI = typeof(EntityFrameworkQueryableExtensions).GetMethods().Where(me => me.Name == methodName).FirstOrDefault();
            MethodCallExpression resultExp = Expression.Call(methodMI.DeclaringType, methodName,
                            parmTypes.ToArray(),
                            queryableExpr, Expression.Quote(selector));
            return resultExp;
        }
        private static Expression IncludeString(Expression queryableExpr, string propertyName)
        {
            MethodCallExpression resultExp = GenerateMethodCall(queryableExpr, "Include", propertyName);
            return resultExp;
        }
        private static Expression ThenIncludeString(Expression queryableExpr, string propertyName, Type parmElementType)
        {
            MethodCallExpression resultExp = GenerateMethodCall(queryableExpr, "ThenInclude", propertyName, parmElementType);
            return resultExp;
        }

        public virtual IQueryable RemoveOrderBy(IQueryable queryable)
        {
            var result = queryable;
            // remove any existing ordering
            var orderVisitor = new OrderByRemover();
            var unorderedExpr = orderVisitor.Visit(result.Expression);
            if (orderVisitor.modified)
                result = result.Provider.CreateQuery(unorderedExpr);
            return result;
        }

        public virtual IQueryable ApplyOrderBy(IQueryable queryable, ODataQueryOptions queryOptions)
		{
			var elementType = TypeFns.GetElementType(queryable.GetType());
			var result = queryable;

            var orderByString = queryOptions.RawValues.OrderBy;
            if (!string.IsNullOrEmpty(orderByString))
            {
                // apply the new order
                var orderByClauses = orderByString.Split(',').ToList();
                var isThenBy = false;
                orderByClauses.ForEach(obc =>
                {
                    var func = QueryBuilder.BuildOrderByFunc(isThenBy, elementType, obc);
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
        public static Func<IQueryable, IQueryable> BuildIQueryableFunc<TArg>(List<Type> instanceTypes, MethodInfo method, TArg parameter, Type queryableBaseType = null)
        {
            if (queryableBaseType == null)
            {
                queryableBaseType = typeof(IQueryable<>);
            }
            var paramExpr = Expression.Parameter(typeof(IQueryable));
            var queryableType = queryableBaseType.MakeGenericType(instanceTypes.ToArray());
            var castParamExpr = Expression.Convert(paramExpr, queryableType);


            var callExpr = Expression.Call(method, castParamExpr, Expression.Constant(parameter));
            var castResultExpr = Expression.Convert(callExpr, typeof(IQueryable));
            var lambda = Expression.Lambda(castResultExpr, paramExpr);
            var func = (Func<IQueryable, IQueryable>)lambda.Compile();
            return func;
        }

        public virtual IQueryable ApplyFilter(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            if (!String.IsNullOrEmpty(queryOptions.RawValues.Filter))
            {
                // apply the odata filter
                queryable = queryOptions.Filter.ApplyTo(queryable as dynamic, this.querySettings);

                // null comparison fixup for Entity Framework
                // Inspired from: http://blog.davidebbo.com/2011/08/how-odata-quirk-killed-nuget-server.html
                var efVisitor = new EFCoreCompatibleVisitor();
                var efExpr = efVisitor.Visit(queryable.Expression);
                // create new query with fixed up expression
                queryable = queryable.Provider.CreateQuery(efExpr);
            }
            return queryable;
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

        private static MethodInfo _toListAsynchMI = null;

        /// <summary>
        /// Replaces the response.Content with the query results, wrapped in a QueryResult object if necessary.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="responseObject"></param>
        /// <param name="queryable"></param>
        public virtual async Task<bool> WrapResult(HttpRequestMessage request, HttpResponseMessage response, IQueryable queryResult)
		{
			//Object tmp;
			//request.Properties.TryGetValue("MS_InlineCount", out tmp);
			//var inlineCount = (Int64?)tmp;

			// if a select or expand was encountered we need to
			// execute the DbQueries here, so that any exceptions thrown can be properly returned.
			// if we wait to have the query executed within the serializer, some exceptions will not
			// serialize properly.

			dynamic listQueryResult = null;
			try
			{
                // try entity core method first
                if (_toListAsynchMI == null)
                    _toListAsynchMI = typeof(EntityFrameworkQueryableExtensions).GetMethods().Where(me => me.Name == "ToListAsync" && me.GetParameters().Count() == 2).FirstOrDefault();
                var countQEntityType = TypeFns.GetElementType(queryResult.Expression.Type);
                var methodMI = _toListAsynchMI.MakeGenericMethod(countQEntityType);

                var resultT = methodMI.Invoke(queryResult, new Object[] { queryResult, new System.Threading.CancellationToken() }) as Task;
                await resultT;
                listQueryResult = (resultT as dynamic).Result;

            }
            catch (Exception ex)
			{
                try
                {
                    // try old method on EF core failure as not every query will be an EF core query
                    listQueryResult = Enumerable.ToList((dynamic)queryResult);
                }
                catch (Exception ex2)
                {
                    throw ex2;
                }
			}

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

            if (listQueryResult != null || this._inlineCountT != null)
			{
				Object result = listQueryResult;
				if (this._inlineCountT != null)
				{
                    var inlineCount = await this._inlineCountT;
					result = new QueryResult() { Results = listQueryResult, InlineCount = inlineCount };
				}

				var formatter = ((dynamic)response.Content).Formatter;
				var oc = new ObjectContent(result.GetType(), result, formatter);
				response.Content = oc;
			}
            return true;
		}


		/// <summary>
		/// Configure the JsonFormatter.  Does nothing in this implementation but is available to derived classes.
		/// </summary>
		/// <param name="request">Used to retrieve the current JsonFormatter</param>
		/// <param name="queryable">Used to obtain the ISession</param>
		public virtual void ConfigureFormatter(HttpRequestMessage request, IQueryable queryable)
		{
			var jsonFormatter = request.GetConfiguration().Formatters.JsonFormatter;
			ConfigureFormatter(jsonFormatter, queryable);
		}

		/// <summary>
		/// Configure the JsonFormatter.  Does nothing in this implementation but is available to derived classes.
		/// </summary>
		/// <param name="jsonFormatter"></param>
		/// <param name="queryable">Used to obtain the ISession</param>
		public virtual void ConfigureFormatter(JsonMediaTypeFormatter jsonFormatter, IQueryable queryable)
		{
		}

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