using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    /// <summary> Gets the minimum Take() value in the queryable Expression </summary>
    public static int? GetTakeValue(IQueryable queryable) {
      var visitor = new TakeFinder();
      visitor.Visit(queryable.Expression);
      return visitor.MinTake;
    }
  }


  /// <summary> Sets MinTake to the minimum Take() value in the queryable Expression </summary>
  public class TakeFinder : ExpressionVisitor {
    public int? MinTake { get; private set; } = null;

    protected override Expression VisitMethodCall(MethodCallExpression node) {
      if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Take") {
        if (int.TryParse(node.Arguments.Last().ToString(), out int arg)) {
          if (MinTake == null || MinTake > arg) {
            MinTake = arg;
          }
        }
      }
      return base.VisitMethodCall(node);
    }
  }
}
