using Breeze.ContextProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Query {
  public class FnExpression : BaseExpression {
    private String _fnName;
    private List<BaseExpression> _exprs;

    // first DataType in the list is the return type the rest are argument
    // types;
    private static Dictionary<String, DataType[]> _fnMap = new Dictionary<String, DataType[]>();
    static FnExpression() {
      RegisterFn("toupper", DataType.String, DataType.String);
      RegisterFn("tolower", DataType.String, DataType.String);
      RegisterFn("trim", DataType.String, DataType.String);
      RegisterFn("concat", DataType.String, DataType.String, DataType.String);
      RegisterFn("substring", DataType.String, DataType.String,
              DataType.Int32, DataType.Int32);
      RegisterFn("replace", DataType.String, DataType.String, DataType.String);
      RegisterFn("length", DataType.Int32, DataType.String);
      RegisterFn("indexof", DataType.Int32, DataType.String, DataType.String);

      RegisterFn("year", DataType.Int32, DataType.DateTime);
      RegisterFn("month", DataType.Int32, DataType.DateTime);
      RegisterFn("day", DataType.Int32, DataType.DateTime);
      RegisterFn("minute", DataType.Int32, DataType.DateTime);
      RegisterFn("second", DataType.Int32, DataType.DateTime);

      RegisterFn("round", DataType.Int32, DataType.Double);
      RegisterFn("ceiling", DataType.Int32, DataType.Double);
      RegisterFn("floor", DataType.Int32, DataType.Double);

      RegisterFn("substringof", DataType.Boolean, DataType.String,
              DataType.String);
      RegisterFn("startsWith", DataType.Boolean, DataType.String,
              DataType.String);
      RegisterFn("endsWith", DataType.Boolean, DataType.String,
              DataType.String);
    }

    public FnExpression(String fnName, List<BaseExpression> exprs) {
      _fnName = fnName;
      _exprs = exprs;
    }

    public static FnExpression CreateFrom(String source, Type entityType) {
      return FnExpressionToken.ToExpression(source, entityType);
    }

    public String FnName {
      get { return _fnName; }
    }

    public List<BaseExpression> Expressions {
      get { return _exprs; }
    }

    public override DataType DataType {
      get {
        return GetReturnType(_fnName);
      }
    }

    public static void RegisterFn(String name, params DataType[] dataTypes) {
      _fnMap[name.ToLowerInvariant()] = dataTypes;
    }

    public static DataType GetReturnType(String fnName) {
      DataType[] dataTypes = _fnMap[fnName.ToLowerInvariant()];
      return (dataTypes != null) ? dataTypes[0] : null;
    }

    public static List<DataType> GetArgTypes(String fnName) {
      DataType[] dataTypes = _fnMap[fnName.ToLowerInvariant()];
      if (dataTypes == null) {
        throw new Exception("Unable to recognize a function named: "
                + fnName);
      }
      return dataTypes.Skip(1).ToList();
    }

    public override Expression ToExpression(Expression inExpr) {
      var expr = _exprs[0].ToExpression(inExpr);
      // TODO: add the rest ...
      if (FnName == "toupper") {
        var mi = TypeFns.GetMethodByExample((String s) => s.ToUpper());
        return Expression.Call(expr, mi);
      } else if (FnName == "tolower") {
        var mi = TypeFns.GetMethodByExample((String s) => s.ToLower());
        return Expression.Call(expr, mi);
      } else if (FnName == "length") {
        return GetPropertyExpression(expr, "Length", typeof(int));
      } else if (FnName == "substring") {
        var exprs = _exprs.Select(e => e.ToExpression(inExpr));
        var mi = TypeFns.GetMethodByExample((String s) => s.Substring(1, 5));
        return Expression.Call(inExpr, mi, exprs);
      } else if (FnName == "year") {
        return GetPropertyExpression(expr, "Year", typeof(int));
        //if (TypeFns.IsNullableType(expr.Type)) {
        //  var nullBaseExpression = Expression.Constant(null, expr.Type);
        //  var test = Expression.Equal(expr, nullBaseExpression);
        //  expr = Expression.Convert(expr, TypeFns.GetNonNullableType(expr.Type));
        //  Expression propExpr = Expression.PropertyOrField(expr, "Year");
        //  propExpr = Expression.Convert(propExpr, TypeFns.GetNullableType(typeof(int)));
        //  var nullExpr = Expression.Constant(null, TypeFns.GetNullableType(typeof(int)));
        //  return Expression.Condition(test, nullExpr, propExpr);
        //} else {
        //  return Expression.PropertyOrField(expr, "Year");
        //}
      } else if (FnName == "month") {
        return GetPropertyExpression(expr, "Month", typeof(int));
      } else {
        throw new Exception("Unable to locate Fn: " + FnName);
      }
    }

    private Expression GetPropertyExpression(Expression expr, string propertyName, Type returnType) {
      if (TypeFns.IsNullableType(expr.Type)) {
        var nullBaseExpression = Expression.Constant(null, expr.Type);
        var test = Expression.Equal(expr, nullBaseExpression);
        expr = Expression.Convert(expr, TypeFns.GetNonNullableType(expr.Type));
        Expression propExpr = Expression.PropertyOrField(expr, propertyName);
        propExpr = Expression.Convert(propExpr, TypeFns.GetNullableType(returnType));
        var nullExpr = Expression.Constant(null, TypeFns.GetNullableType(returnType));
        return Expression.Condition(test, nullExpr, propExpr);
      } else {
        return Expression.PropertyOrField(expr, propertyName);
      }
    }
  }
}