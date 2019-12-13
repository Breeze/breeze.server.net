using Breeze.Core;
using NHibernate;
using NHibernate.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Breeze.Persistence.NH {
  public static class NHQueryHelper {

    public static IQueryable<TEntity> Include<TEntity>(this IQueryable<TEntity> source, string navigationPropertyPath) where TEntity : class {
      if (source == null) return source;
      var provider = source.Provider as DefaultQueryProvider;
      if (provider == null) return source;
      if (provider is NHQueryProvider) {
        ((NHQueryProvider)provider).Includes.Add(navigationPropertyPath);
        return source;
      } else {
        var nhp = new NHQueryProvider(provider);
        nhp.Includes.Add(navigationPropertyPath);
        var q = nhp.CreateQuery<TEntity>(source.Expression);
        return q;
      }
    }

    public static bool NeedsExecution(string queryString, IQueryable queryable) {
      return (queryable != null && (queryString != null || queryable.Provider is DefaultQueryProvider));
    }

    public static IList PostExecuteQuery(this EntityQuery eq, IQueryable queryable, IList result) {
      var expands = new List<string>();
      var provider = queryable.Provider as NHQueryProvider;
      if (provider != null && provider.Includes != null) {
        expands.AddRange(provider.Includes);
      }
      if (eq != null && eq.ExpandClause != null) {
        expands.AddRange(eq.ExpandClause.PropertyPaths);
      }
      if (expands.Count > 0) {
        NHExpander.InitializeList(result, expands);
      }

      if (queryable != null) {
        var session = GetSession(queryable);
        if (session != null) {
          if (session.IsOpen) session.Close();
        }
      }

      return result;
    }

    /// <summary>
    /// Get the ISession from the IQueryable.
    /// </summary>
    /// <param name="queryable"></param>
    /// <returns>the session if queryable.Provider is NHibernate.Linq.DefaultQueryProvider, else null</returns>
    private static ISession GetSession(IQueryable queryable) {
      if (queryable == null) return null;
      var provider = queryable.Provider as DefaultQueryProvider;
      if (provider == null) return null;
      var sessionImpl = provider.Session as NHibernate.Impl.SessionImpl;
      return sessionImpl;
    }

  }
}
