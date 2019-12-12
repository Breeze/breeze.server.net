using Breeze.Core;
using NHibernate;
using NHibernate.Linq;
using System.Collections;
using System.Linq;

namespace Breeze.Persistence.NH {
  public static class NHQueryHelper {

    public static bool NeedsExecution(string queryString, IQueryable queryable) {
      return (queryable != null && (queryString != null || queryable.Provider is DefaultQueryProvider));
    }

    public static IList PostExecuteQuery(this EntityQuery eq, IQueryable queryable, IList result) {
      if (eq != null && eq.ExpandClause != null) {
        var expandPaths = eq.ExpandClause.PropertyPaths.ToArray();
        NHExpander.InitializeList(result, expandPaths);
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
