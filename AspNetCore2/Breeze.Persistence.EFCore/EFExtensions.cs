using Breeze.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Breeze.Persistence.EFCore {


  public static class EFExtensions {

    public static IQueryable ApplyExpand(this EntityQuery eq, IQueryable queryable, Type eleType) {
      if (eq.ExpandClause != null) {
        eq.ExpandClause.PropertyPaths.ToList().ForEach(expand => {
          queryable = EFQueryBuilder.ApplyExpand(queryable, eleType, expand.Replace('/', '.'));
        });
      }
      return queryable;
    }
  }

  public class EFQueryBuilder {

    public static IQueryable ApplyExpand(IQueryable source, Type elementType, string expand) {
      var method = TypeFns.GetMethodByExample((IQueryable<String> q) => 
        EntityFrameworkQueryableExtensions.Include<String>(q, "dummyPath"), elementType);
      var func = QueryBuilder.BuildIQueryableFunc(elementType, method, expand);
      return func(source);
    }

  }
}

