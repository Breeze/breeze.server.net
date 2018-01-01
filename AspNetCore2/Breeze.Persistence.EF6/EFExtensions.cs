using Breeze.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Breeze.Persistence.EF6 {


  public static class EFExtensions {

    public static IQueryable ApplyExpand(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.ExpandClause != null) {
        eq.ExpandClause.PropertyPaths.ToList().ForEach(expand => {
          queryable = (IQueryable) ((dynamic) queryable).Include(expand);
        });
      }
      return queryable;
    }
  }

  
}

