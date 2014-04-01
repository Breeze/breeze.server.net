using NHibernate.Engine;
using NHibernate.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace Breeze.ContextProvider.NH
{
    /// <summary>
    /// Extends NhQueryable to add an Include function.  
    /// 
    /// Include supports the OData $expand implementation, which retrieves related entities by following navigation properties.
    /// Note that Include is not the same as Fetch.  Fetch performs a join operation, while Include causes as second query.
    /// Fetch can be faster, but Include preserves the row count sematics of the original query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NhQueryableInclude<T> : NhQueryable<T>, IQueryableInclude
    {
        private List<string> includes;

        /// <summary>
        /// Create a query which may be marked cachable.
        /// 
        /// Calls the base constructor, setting the Expression using NhQueryable.
        /// Needed because the NHibernate Linq parser chokes if NhQueryableInclude is in the expression.
        /// </summary>
        /// <remarks>Note that some combinations of operations (.Select with .Where) result in errors 
        /// from the NHibernate LINQ provider when using cacheable queries.</remarks>
        /// <param name="si">Session</param>
        /// <param name="cacheable">True to set the query as cachable, false as not cacheable.  Defaults to false.</param>
        public NhQueryableInclude(ISessionImplementor si, bool cacheable = false)
            : base(new DefaultQueryProvider(si), Expression.Constant(cacheable ? new NhQueryable<T>(si).Cacheable<T>() : new NhQueryable<T>(si)))
        {}

        /// <summary>
        /// Create a cacheable query using the given cache region.
        /// 
        /// Calls the base constructor, setting the Expression using NhQueryable.
        /// Needed because the NHibernate Linq parser chokes if NhQueryableInclude is in the expression.
        /// </summary>
        /// <remarks>Note that some combinations of operations (.Select with .Where) result in errors 
        /// from the NHibernate LINQ provider when using cacheable queries.</remarks>
        /// <param name="si">Session</param>
        /// <param name="cacheRegion">Cache Region to use for caching the query.  
        /// <see cref="http://nhforge.org/doc/nh/en/#performance-querycache"/> and
        /// <see cref="http://nhforge.org/doc/nh/en/#caches"/></param>
        public NhQueryableInclude(ISessionImplementor si, string cacheRegion)
            : base(new DefaultQueryProvider(si), Expression.Constant(new NhQueryable<T>(si).Cacheable<T>().CacheRegion(cacheRegion)))
        { }

        public NhQueryableInclude(IQueryProvider provider, Expression expr) : base(provider, expr)
        {}

        public IList<string> GetIncludes()
        {
            return includes;
        }

        /// <summary>
        /// Allows Include clauses to be added to NhQueryable objects.
        /// </summary><example>
        /// var query = new NhQueryableInclude<Customer>(session.GetSessionImplementation());
        /// query = query.Include("Orders");
        /// </example>
        /// <param name="propertyPath"></param>
        /// <returns></returns>
        public NhQueryableInclude<T> Include(string propertyPath)
        {
            if (includes == null) includes = new List<string>();
            includes.Add(propertyPath);

            return this;
        }

    }

    public interface IQueryableInclude : IQueryable
    {
        IList<string> GetIncludes();
    }
}
