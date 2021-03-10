using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;


namespace Breeze.Core {

  public class PropertySignature {
    public PropertySignature(Type instanceType, String propertyPath) {
      InstanceType = instanceType;
      PropertyPath = propertyPath;
      Properties = GetProperties(InstanceType, PropertyPath).ToList();
    }

    public static bool IsProperty(Type instanceType, String propertyPath) {
      return GetProperties(instanceType, propertyPath, false).Any(pi => pi != null);
    }

    public Type InstanceType { get; private set; }
    public String PropertyPath { get; private set; }
    public List<PropertyInfo> Properties { get; private set; }

    public String Name {
      get { return Properties.Select(p => p.Name).ToAggregateString("_"); }
    }

    public Type ReturnType {
      get { return Properties.Last().PropertyType; }
    }

    // returns null for scalar properties
    public Type ElementType {
      get { return TypeFns.GetElementType(ReturnType); }

    }

    public bool IsDataProperty {
      get { return TypeFns.IsPredefinedType(ReturnType) || TypeFns.IsEnumType(ReturnType); }
    }

    public bool IsNavigationProperty {
      get { return !IsDataProperty; }
    }



    // returns an IEnumerable<PropertyInfo> with nulls if invalid and throwOnError = true
    public static IEnumerable<PropertyInfo> GetProperties(Type instanceType, String propertyPath, bool throwOnError = true) {
      var propertyNames = propertyPath.Split('.');

      var nextInstanceType = instanceType;
      foreach (var propertyName in propertyNames) {
        var property = GetProperty(nextInstanceType, propertyName, throwOnError);
        if (property != null) {
          yield return property;

          nextInstanceType = property.PropertyType;
        } else {
          break;
        }
      }
    }

    private static PropertyInfo GetProperty(Type instanceType, String propertyName, bool throwOnError = true) {
      var propertyInfo = (PropertyInfo)TypeFns.FindPropertyOrField(instanceType, propertyName,
        BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
      if (propertyInfo == null) {
        if (throwOnError) {
          var msg = String.Format("Unable to locate property '{0}' on type '{1}'.", propertyName, instanceType);
          throw new Exception(msg);
        } else {
          return null;
        }
      }
      return propertyInfo;
    }

    public Expression BuildMemberExpression(ParameterExpression parmExpr) {
      Expression memberExpr = BuildPropertyExpression(parmExpr, Properties.First());
      foreach (var property in Properties.Skip(1)) {
        memberExpr = BuildPropertyExpression(memberExpr, property);
      }
      return memberExpr;
    }

    public Expression BuildPropertyExpression(Expression baseExpr, PropertyInfo property) {
      return Expression.Property(baseExpr, property);
    }



  }


}
