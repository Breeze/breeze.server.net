
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Core {
  public class Operator {
    public static Dictionary<String, Operator> _opMap = new Dictionary<String, Operator>();

    public static Operator Any = new Operator("any,some", OperatorType.AnyAll);
    public static Operator All = new Operator("all,every", OperatorType.AnyAll);
    public static Operator And = new Operator("and,&&", OperatorType.AndOr);
    public static Operator Or = new Operator("or,||", OperatorType.AndOr);
    public static Operator Not = new Operator("not,!", OperatorType.Unary);

    public static new BinaryOperator Equals = new BinaryOperator("eq,==");
    public static BinaryOperator NotEquals = new BinaryOperator("ne,!=");
    public static BinaryOperator LessThan = new BinaryOperator("lt,<");
    public static BinaryOperator LessThanOrEqual = new BinaryOperator("le,<=");
    public static BinaryOperator GreaterThan = new BinaryOperator("gt,>");
    public static BinaryOperator GreaterThanOrEqual = new BinaryOperator("ge,>=");

    public static BinaryOperator StartsWith = new BinaryOperator("startswith");
    public static BinaryOperator EndsWith = new BinaryOperator("endswith");
    public static BinaryOperator Contains = new BinaryOperator("contains");

    public static BinaryOperator In = new BinaryOperator("in");
        
    public String Name { get; private set; }
    public OperatorType OpType { get; private set; }
    public List<String> _aliases;

    public static Operator FromString(String op) {
      if (_opMap.ContainsKey(op.ToLowerInvariant())) {
        return _opMap[op.ToLowerInvariant()];
      } else {
        return null;
      }
    }

    public Operator(String aliases, OperatorType opType) {
      _aliases = aliases.Split(',').ToList();
      Name = _aliases[0];
      OpType = opType;
      AddOperator(this);
    }


    private static void AddOperator(Operator op) {
      op._aliases.ForEach(a => _opMap[op.Name.ToLowerInvariant()] = op);
    }
  }

  public class BinaryOperator : Operator {
    public Expression Expression { get; private set; }
    public BinaryOperator(String name) : base(name, OperatorType.Binary) {
      
    }

  }
}
