using System;
using System.Linq.Expressions;

namespace Breeze.Core {

  public class UnaryPredicate : BasePredicate {
    
    public BasePredicate Predicate { get; private set; }
  
    public UnaryPredicate(Operator op, BasePredicate predicate) : base(op) {
      Predicate = predicate;
    }
  

    public override void Validate(Type entityType) {
      Predicate.Validate(entityType);
    }


    public override Expression ToExpression(ParameterExpression paramExpr) {
      var expr = Predicate.ToExpression(paramExpr);
      return Expression.Not(expr);
    }
  }

  }