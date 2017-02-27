
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Core {
  public class FnBlock : BaseBlock {
    public String FnName { get; private set; }
    private List<BaseBlock> _exprs;

    // first DataType in the list is the return type the rest are argument
    // types;
    private static Dictionary<String, DataType[]> _fnMap = new Dictionary<String, DataType[]>();
    static FnBlock() {
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
      RegisterFn("hour", DataType.Int32, DataType.DateTime);
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

    public FnBlock(String fnName, List<BaseBlock> exprs) {
      FnName = fnName;
      _exprs = exprs;
    }

    public static FnBlock CreateFrom(String source, Type entityType) {
      return FnBlockToken.ToExpression(source, entityType);
    }

    public IEnumerable<BaseBlock> Expressions {
      get { return _exprs.AsReadOnly(); }
    }

    public override DataType DataType {
      get {
        return GetReturnType(FnName);
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
      var exprs = _exprs.Select(e => e.ToExpression(inExpr)).ToList();
      var expr = exprs[0];
      // TODO: add the rest ...
      if (FnName == "toupper") {
        var mi = TypeFns.GetMethodByExample((String s) => s.ToUpper());
        return Expression.Call(expr, mi);
      } else if (FnName == "tolower") {
        var mi = TypeFns.GetMethodByExample((String s) => s.ToLower());
        return Expression.Call(expr, mi);
      } else if (FnName == "trim") {
        var mi = TypeFns.GetMethodByExample((String s) => s.Trim());
        return Expression.Call(expr, mi);
      } else if (FnName == "length") {
        return GetPropertyExpression(expr, "Length", typeof(int));
      } else if (FnName == "indexof") {
        var mi = TypeFns.GetMethodByExample((String s) => s.IndexOf("xxx"));
        return Expression.Call(exprs[0], mi, exprs[1]);
      } else if (FnName == "concat") {
        // TODO: check if this works...
        var mi = TypeFns.GetMethodByExample((String s) => String.Concat(s, "xxx"));
        return Expression.Call(mi, exprs[0], exprs[1]);
      } else if (FnName == "substring") {
        var mi = TypeFns.GetMethodByExample((String s) => s.Substring(1, 5));
        return Expression.Call(exprs[0], mi, exprs.Skip(1));
      } else if (FnName == "replace") {
        // TODO: check if this works...
        var mi = TypeFns.GetMethodByExample((String s) => s.Replace("aaa", "bbb"));
        return Expression.Call(exprs[0], mi, exprs[1], exprs[2]);
      } else if (FnName == "year") {
        return GetPropertyExpression(expr, "Year", typeof(int));
      } else if (FnName == "month") {
        return GetPropertyExpression(expr, "Month", typeof(int));
      } else if (FnName == "day") {
        return GetPropertyExpression(expr, "Day", typeof(int));
      } else if (FnName == "hour") {
        return GetPropertyExpression(expr, "Hour", typeof(int));
      } else if (FnName == "minute") {
        return GetPropertyExpression(expr, "Minute", typeof(int));
      } else if (FnName == "second") {
        return GetPropertyExpression(expr, "Second", typeof(int));
      } else if (FnName == "round") {
          // TODO: confirm that this works - is using static method.
          var mi = TypeFns.GetMethodByExample((Double d) => Math.Round(d));
          return Expression.Call(mi, expr);
      } else if (FnName == "ceiling") {
        var mi = TypeFns.GetMethodByExample((Double d) => Math.Ceiling(d));
        return Expression.Call(mi, expr);
      } else if (FnName == "floor") {
        var mi = TypeFns.GetMethodByExample((Double d) => Math.Floor(d));
        return Expression.Call(mi, expr);
      } else if (FnName == "startswith") {
        var mi = TypeFns.GetMethodByExample((String s) => s.StartsWith("xxx"));
        return Expression.Call(exprs[0], mi, exprs[1]);
      } else if (FnName == "endsWith") {
        var mi = TypeFns.GetMethodByExample((String s) => s.EndsWith("xxx"));
        return Expression.Call(exprs[0], mi, exprs[1]);
      } else if (FnName == "substringof") {
        var mi = TypeFns.GetMethodByExample((String s) => s.Contains("xxx"));
        return Expression.Call(exprs[0], mi, exprs[1]);
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