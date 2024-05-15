using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Breeze.AspNetCore.NetCore {
  internal static class QueryableExtensions {
    /// <summary> Create a list from an IAsyncEnumerable; fallback to ToList() if source is not IAsyncEnumberable. </summary>
    // from https://github.com/dotnet/efcores/blob/c6b5eac69fb2ec5dfdb4b990837d8cfdd91753a2/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L2298
    internal static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) {
      if (source is IAsyncEnumerable<TSource> asyncEnumerable) {
        var list = new List<TSource>();
        await foreach (var element in asyncEnumerable.WithCancellation(cancellationToken)) {
          list.Add(element);
        }
        return list;
      } else {
        return source.ToList();
      }
    }

    /// <summary> Run the Count operation on another thread </summary>
    internal static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) {
      return Task.Run(() => source.Count(), cancellationToken);
     }
  }
}
