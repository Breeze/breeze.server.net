using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Breeze.AspNetCore.NetCore {
  internal static class QueryableExtensions {
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
  }
}
