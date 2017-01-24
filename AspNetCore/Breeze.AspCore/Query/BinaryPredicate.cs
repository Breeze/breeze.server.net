using Breeze.ContextProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Query {
  /**
   * Represents a where clause that compares two values given a specified operator, the two values are either a property
   * and a literal value, or two properties.  
   * @author IdeaBlade
   *
   */
  public class BinaryPredicate : BasePredicate {
    private Object _expr1Source;
    private Object _expr2Source;
    private BaseExpression _expr1;
    private BaseExpression _expr2;

    public BinaryPredicate(Operator op, Object expr1Source, Object expr2Source) {
      _op = op;
      _expr1Source = expr1Source;
      _expr2Source = expr2Source;
    }


    public Object getExpr1Source() {
      return _expr1Source;
    }

    public Object getExpr2Source() {
      return _expr2Source;
    }

    public BaseExpression getExpr1() {
      return _expr1;
    }

    public BaseExpression getExpr2() {
      return _expr2;
    }

    public override void Validate(Type entityType) {
      if (_expr1Source == null) {
        throw new Exception("Unable to validate 1st expression: " + this._expr1Source);
      }

      this._expr1 = BaseExpression.CreateLHSExpression(_expr1Source, entityType);

      if (_op == Operator.In && !(_expr2Source is IList)) {
        throw new Exception("The 'in' operator requires that its right hand argument be an array");
      }

      // Special purpose Enum handling

      var enumType = GetEnumType(this._expr1);
      if (enumType != null && _expr2Source != null) {
        var expr2Enum = Enum.Parse(enumType, (String)_expr2Source);
        this._expr2 = BaseExpression.CreateRHSExpression(expr2Enum, entityType, null);
      } else {
        this._expr2 = BaseExpression.CreateRHSExpression(_expr2Source, entityType, this._expr1.DataType);
      }
    }

    private Type GetEnumType(BaseExpression expr) {
      if (expr is PropExpression) {
        PropExpression pExpr = (PropExpression)expr;
        var prop = pExpr.Property;
        if (prop.IsDataProperty) {
          if (TypeFns.IsEnumType(prop.ReturnType)) {
            return prop.ReturnType;
          }
        }
      }
      return null;
    }

    public override Expression ToExpression(ParameterExpression paramExpr) {
      return BuildBinaryExpr(_expr1.ToExpression(paramExpr), _expr2.ToExpression(paramExpr), Operator);
    }

    private Expression BuildBinaryExpr(Expression expr1, Expression expr2, Operator op) {

      if (expr1.Type != expr2.Type) {
        if (TypeFns.IsNullableType(expr1.Type) && !TypeFns.IsNullableType(expr2.Type)) {
          expr2 = Expression.Convert(expr2, expr1.Type);
        } else if (TypeFns.IsNullableType(expr2.Type) && !TypeFns.IsNullableType(expr1.Type)) {
          expr1 = Expression.Convert(expr1, expr2.Type);
        }

        if (HasNullValue(expr2) && CannotBeNull(expr1)) {
          expr1 = Expression.Convert(expr1, TypeFns.GetNullableType(expr1.Type));
        } else if (HasNullValue(expr1) && CannotBeNull(expr2)) {
          expr2 = Expression.Convert(expr2, TypeFns.GetNullableType(expr2.Type));
        }
        
      }



      if (op == BinaryOperator.Equals) {
        return Expression.Equal(expr1, expr2);
      } else if (op == BinaryOperator.NotEquals) {
        return Expression.NotEqual(expr1, expr2);
      } else if (op == BinaryOperator.GreaterThan) {
        return Expression.GreaterThan(expr1, expr2);
      } else if (op == BinaryOperator.GreaterThanOrEqual) {
        return Expression.GreaterThanOrEqual(expr1, expr2);
      } else if (op == BinaryOperator.LessThan) {
        return Expression.LessThan(expr1, expr2);
      } else if (op == BinaryOperator.LessThanOrEqual) {
        return Expression.LessThanOrEqual(expr1, expr2);
      } else if (op == BinaryOperator.StartsWith) {
        var mi = TypeFns.GetMethodByExample((String s) => s.StartsWith("abc"));
        return Expression.Call(expr1, mi, expr2);
      } else if (op == BinaryOperator.EndsWith) {
        var mi = TypeFns.GetMethodByExample((String s) => s.EndsWith("abc"));
        return Expression.Call(expr1, mi, expr2);
      } else if (op == BinaryOperator.Contains) {
        var mi = TypeFns.GetMethodByExample((String s) => s.Contains("abc"));
        return Expression.Call(expr1, mi, expr2);
      } else if (op == BinaryOperator.In) {
        // TODO: need to generalize this past just 'string'
        var mi = TypeFns.GetMethodByExample((List<String> list) => list.Contains("abc"), expr1.Type);
        return Expression.Call(expr2, mi, expr1);
      }

      return null;
    }
    
    private bool HasNullValue(Expression expr) {
      var le = expr as ConstantExpression;
      return le == null ? false : le.Value == null;
    }

    private bool CannotBeNull(Expression expr) {
      var t = expr.Type;
      return TypeFns.IsPredefinedType(t) && t != typeof(String);
    }
  }
}