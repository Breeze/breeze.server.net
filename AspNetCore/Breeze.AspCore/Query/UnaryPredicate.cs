using System;
using System.Linq.Expressions;

namespace Breeze.Query {

  public class UnaryPredicate : BasePredicate {
    
    private BasePredicate _predicate;
  
    public UnaryPredicate(Operator op, BasePredicate predicate) {
      _op = op;
      _predicate = predicate;
    }
  
    
    public BasePredicate Predicate {
      get { return _predicate; }
    }

    public override Expression ToExpression(ParameterExpression paramExpr) {
      var expr = _predicate.ToExpression(paramExpr);
      return Expression.Not(expr);
    }

    public override void Validate(Type entityType) {
      _predicate.Validate(entityType);
    }
  }

  }