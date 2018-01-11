
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Core {
  /**
   * Represents a where clause that compares two values given a specified operator, the two values are either a property
   * and a literal value, or two properties.  
   * @author IdeaBlade
   *
   */
  public class BinaryPredicate : BasePredicate {
    public Object Expr1Source { get; private set; }
    public Object Expr2Source { get; private set; }
    private BaseBlock _block1;
    private BaseBlock _block2;

    public BinaryPredicate(Operator op, Object expr1Source, Object expr2Source) : base(op) {
      Expr1Source = expr1Source;
      Expr2Source = expr2Source;
    }

    public override void Validate(Type entityType) {
      if (Expr1Source == null) {
        throw new Exception("Unable to validate 1st expression: " + this.Expr1Source);
      }

      this._block1 = BaseBlock.CreateLHSBlock(Expr1Source, entityType);

      if (_op == Operator.In && !(Expr2Source is IList)) {
        throw new Exception("The 'in' operator requires that its right hand argument be an array");
      }

      // Special purpose Enum handling

      var enumType = GetEnumType(this._block1);
      if (enumType != null) { 
        if (Expr2Source != null) {
          var et = TypeFns.GetNonNullableType(enumType);
          var expr2Enum = Enum.Parse(et, (String)Expr2Source);
          this._block2 = BaseBlock.CreateRHSBlock(expr2Enum, entityType, null);
        } else {
          this._block2 = BaseBlock.CreateRHSBlock(null, entityType, null);
        }
      } else {
        this._block2 = BaseBlock.CreateRHSBlock(Expr2Source, entityType, this._block1.DataType);
      }
    }



    public override Expression ToExpression(ParameterExpression paramExpr) {
      return BuildBinaryExpr(_block1.ToExpression(paramExpr), _block2.ToExpression(paramExpr), Operator);
    }

    private Type GetEnumType(BaseBlock block) {
      if (block is PropBlock) {
        PropBlock pExpr = (PropBlock)block;
        var prop = pExpr.Property;
        if (prop.IsDataProperty) {
          if (TypeFns.IsEnumType(prop.ReturnType)) {
            return prop.ReturnType;
          }
        }
      }
      return null;
    }

    private Expression BuildBinaryExpr(Expression expr1, Expression expr2, Operator op) {

      if (expr1.Type != expr2.Type) {
        
        if (TypeFns.IsNullableType(expr1.Type) && !TypeFns.IsNullableType(expr2.Type)) {
          if (!expr2.Type.IsEnum) {
            expr2 = Expression.Convert(expr2, expr1.Type);
          } else {
            expr1 = Expression.Convert(expr1, expr2.Type);
          }
        } else if (TypeFns.IsNullableType(expr2.Type) && !TypeFns.IsNullableType(expr1.Type)) {
          if (!expr1.Type.IsEnum) {
            expr1 = Expression.Convert(expr1, expr2.Type);
          } else {
            expr2 = Expression.Convert(expr2, expr1.Type);
          }
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