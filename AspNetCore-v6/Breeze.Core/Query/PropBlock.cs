
using Breeze.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Core {

  public class PropBlock : BaseBlock {
    public String PropertyPath { get; private set; }
    public PropertySignature Property { get; private set; }
    public Type EntityType { get; private set; } 

    public PropBlock(String propertyPath, Type entityType) {
      EntityType = entityType;
      PropertyPath = propertyPath;
      Property = new PropertySignature(entityType, propertyPath);

      if (Property == null) {
        throw new Exception("Unable to validate propertyPath: " + PropertyPath + " on EntityType: " + entityType.Name);
      }

    }


    public override DataType DataType {
      get {
        try {
          return DataType.FromType(Property.ReturnType);
        } catch {
          throw new Exception("This property expression returns a NavigationProperty not a DataProperty");
        }
      }
    }

    public override Expression ToExpression(Expression inExpr) {
      return Property.BuildMemberExpression((ParameterExpression) inExpr);
    }
  }
}