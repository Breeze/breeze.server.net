using Breeze.ContextProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Breeze.Query {

  public class PropExpression : BaseExpression {
    private String _propertyPath;
    private PropertySignature _property;
    private Type _entityType;

    public PropExpression(String propertyPath, Type entityType) {
      _entityType = entityType;
      _propertyPath = propertyPath;
      _property = new PropertySignature(entityType, propertyPath);

      if (_property == null) {
        throw new Exception("Unable to validate propertyPath: " + _propertyPath + " on EntityType: " + entityType.Name);
      }
    }

    public Type EntityType {
      get { return _entityType; }
    }

    public String PropertyPath {
      get { return _propertyPath; }
    }

    public PropertySignature Property {
      get { return _property; }
    }

    public override DataType DataType {
      get {
        try {
          return DataType.FromType(_property.ReturnType);
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