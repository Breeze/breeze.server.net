using Breeze.ContextProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Query {
  public class AnyAllPredicate : BasePredicate {

    public Object ExprSource { get; private set; }
    public PropExpression Expr { get; private set; } // calculated as a result of validate;
    public BasePredicate Predicate { get; private set; } 


    public AnyAllPredicate(Operator op, Object exprSource, BasePredicate predicate) {
      _op = op;
      ExprSource = exprSource;
      Predicate = predicate;
    }

    

    public override void Validate(Type entityType) {
      var expr = BaseExpression.CreateLHSExpression(ExprSource, entityType);
      if (!(expr is PropExpression)) {
        throw new Exception("The first expression of this AnyAllPredicate must be a PropertyExpression");
      }
      var pexpr = (PropExpression)expr;
      var prop = pexpr.Property;
      if (!prop.IsNavigationProperty) {
        throw new Exception("The first expression of this AnyAllPredicate must be a Navigation PropertyExpression");
      }

      this.Expr = pexpr;
      this.Predicate.Validate(prop.InstanceType);

    }

    public override Expression ToExpression(ParameterExpression paramExpr) {
      var expr = Expr.ToExpression(paramExpr);
      var elementType = TypeFns.GetElementType(Expr.Property.ReturnType);
      var eleParamExpr = Expression.Parameter(elementType);
      var predExpr = Predicate.ToExpression(eleParamExpr);
      // Need to generalize this.
      var mi = TypeFns.GetMethodByExample((List<String> list) => list.Any(x => x != null));
      return Expression.Call(paramExpr, mi, predExpr);
    }
  }
}
