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

    public static IQueryable ApplyAsNoTracking(this EntityQuery eq, IQueryable queryable, Type eleType) {
      // This was added for EF Core 3 because of requirment that all owned types have their owners as part of the query
      // unless we add 'AsNoTracking'
      if (eq.SelectClause != null) {
        // TODO: this isn't quite right - we only want to ApplyAsNoTracking if the result of the select includes an 'owned' type
        // but currently the code just checks for any non system type, so we are applying 'noTracking' more than we should.
        // but we need to apply the AsNoTracking before the select ( much simpler that way).
        var areAllSystemTypes = eq.SelectClause.Properties.All(p => p.ReturnType.FullName.StartsWith("System."));
        if (!areAllSystemTypes) {
          queryable = EFQueryBuilder.ApplyAsNoTracking(queryable, eleType);
        }
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

    public static IQueryable ApplyAsNoTracking(IQueryable source, Type elementType) {
      var method = TypeFns.GetMethodByExample((IQueryable<Object> q) =>
        EntityFrameworkQueryableExtensions.AsNoTracking<Object>(q), elementType);
      var func = QueryBuilder.BuildIQueryableFunc(elementType, method);
      return func(source);
    }


  }
}

