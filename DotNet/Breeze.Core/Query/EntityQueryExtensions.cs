using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Breeze.Core {
  public static class EntityQueryExtensions {
    public static IQueryable ApplyWhere(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.WherePredicate != null) {
        queryable = QueryBuilder.ApplyWhere(queryable, eleType, eq.WherePredicate);
      }
      return queryable;
    }

    public static IQueryable ApplyOrderBy(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.OrderByClause != null) {
        queryable = QueryBuilder.ApplyOrderBy(queryable, eleType, eq.OrderByClause);
      }
      return queryable;
    }

    public static IQueryable ApplySelect(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.SelectClause != null) {
        queryable = QueryBuilder.ApplySelect(queryable, eleType, eq.SelectClause);
      }
      return queryable;
    }

    public static IQueryable ApplySkip(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.SkipCount.HasValue) {
        queryable = QueryBuilder.ApplySkip(queryable, eleType, eq.SkipCount.Value);
      }
      return queryable;
    }

    public static IQueryable ApplyTake(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.TakeCount.HasValue) {
        queryable = QueryBuilder.ApplyTake(queryable, eleType, eq.TakeCount.Value);
      }
      return queryable;
    }

    public static IQueryable ApplyExpand(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.ExpandClause != null) {
        eq.ExpandClause.PropertyPaths.ToList().ForEach(expand => {
          queryable = ((dynamic)queryable).Include(expand.Replace('/', '.'));
        });
        
      }
      return queryable;
    }
    // from https://github.com/dotnet/efcore/blob/c6b5eac69fb2ec5dfdb4b990837d8cfdd91753a2/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L2298
    private static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) {
      var list = new List<TSource>();
      await foreach (var element in source.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
        list.Add(element);
      }

      return list;
    }
    // from https://github.com/dotnet/efcore/blob/c6b5eac69fb2ec5dfdb4b990837d8cfdd91753a2/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L3131
    private static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(
      this IQueryable<TSource> source) {
      if (source is IAsyncEnumerable<TSource> asyncEnumerable) {
        return asyncEnumerable;
      }

      throw new InvalidOperationException("Queryable is not async");
    }
  }
}
