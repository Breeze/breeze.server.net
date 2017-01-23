using Breeze.ContextProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Query {

  public abstract class BaseExpression {

    // will return either a PropExpression or a FnExpression
    public static BaseExpression CreateLHSExpression(Object exprSource,
        Type entityType) {
      if (exprSource == null) {
        throw new Exception(
            "Null expressions are only permitted on the right hand side of a BinaryPredicate");
      }

      if (exprSource is IDictionary) {
        throw new Exception(
            "Object expressions are only permitted on the right hand side of a BinaryPredicate");
      }

      if (exprSource is IList) {
        throw new Exception(
            "Array expressions are only permitted on the right hand side of a BinaryPredicate");
      }

      if (!(exprSource is String)) {
        throw new Exception(
            "Only string expressions are permitted on this predicate");
      }

      String source = (String)exprSource;
      if (source.IndexOf("(") == -1) {
        return new PropExpression(source, entityType);
      } else {
        return FnExpression.CreateFrom(source, entityType);
      }


    }

    // will return either a PropExpression or a LitExpression
    public static BaseExpression CreateRHSExpression(Object exprSource,
        Type entityType, DataType otherExprDataType) {

      if (exprSource == null) {
        return new LitExpression(exprSource, otherExprDataType);
      }

      if (exprSource is String) {
        String source = (String)exprSource;
        if (entityType == null) {
          // if entityType is unknown then assume that the rhs is a
          // literal
          return new LitExpression(source, otherExprDataType);
        }

        if (PropertySignature.IsProperty(entityType, source)) {
          return new PropExpression(source, entityType);
        } else { 
          return new LitExpression(source, otherExprDataType);
        } 
      }

      if (TypeFns.IsPredefinedType(exprSource.GetType())) {
        return new LitExpression(exprSource, otherExprDataType);
      }

      if (exprSource is IDictionary<string, Object>) {
        var exprMap = (IDictionary<string, Object>)exprSource;
        // note that this is NOT the same a using get and checking for null
        // because null is a valid 'value'.
        if (!exprMap.ContainsKey("value")) {
          throw new Exception(
              "Unable to locate a 'value' property on: "
                  + exprMap.ToString());
        }
        Object value = exprMap["value"];

        if (exprMap.ContainsKey("isProperty")) {
          return new PropExpression((String)value, entityType);
        } else {
          String dt = (String)exprMap["dataType"];
          DataType dataType = (dt != null) ? DataType.FromName(dt) : otherExprDataType;
          return new LitExpression(value, dataType);
        }
      }

      if (exprSource is IList) {
        // right now this pretty much implies the values on an 'in' clause
        return new LitExpression(exprSource, otherExprDataType);
      }

      if (TypeFns.IsEnumType(exprSource.GetType())) {
        return new LitExpression(exprSource, otherExprDataType);
      }

      throw new Exception(
          "Unable to parse the right hand side of this BinaryExpression: "
              + exprSource.ToString());

    }

    public abstract DataType DataType {
      get;
    }

    

    public abstract Expression ToExpression(Expression inExpr);

  }

}