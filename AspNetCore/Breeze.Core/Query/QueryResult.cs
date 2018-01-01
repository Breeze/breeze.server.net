using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Breeze.Core {
  public class QueryResult {

    public QueryResult(IEnumerable results, int? inlineCount = null) {
      Results = results;
      InlineCount = inlineCount;
    }

    public IEnumerable Results {
      get; private set;
    }

    public int? InlineCount {
      get; private set;
    }
  }

}
