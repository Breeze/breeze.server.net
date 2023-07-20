using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Breeze.AspNetCore.NetCore
{
  internal static class QueryableExtensions {
    // from https://github.com/dotnet/efcore/blob/c6b5eac69fb2ec5dfdb4b990837d8cfdd91753a2/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L2298
    internal static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) {
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
