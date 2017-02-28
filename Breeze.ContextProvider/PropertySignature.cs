using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections;


namespace Breeze.ContextProvider {

  internal class PropertySignature  {
    public PropertySignature(Type instanceType, String propertyPath) {
      InstanceType = instanceType;
      PropertyPath = propertyPath;
      Properties = GetProperties(InstanceType, PropertyPath).ToList();
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

    private IEnumerable<PropertyInfo> GetProperties(Type instanceType, String propertyPath) {
      var propertyNames = propertyPath.Split('.');

      var nextInstanceType = instanceType;
      foreach (var propertyName in propertyNames) {
        var property = GetProperty(nextInstanceType, propertyName);
        yield return property;
        nextInstanceType = property.PropertyType;
      }
    }

    private PropertyInfo GetProperty(Type instanceType, String propertyName) {
      var propertyInfo = (PropertyInfo)TypeFns.FindPropertyOrField(instanceType, propertyName,
        BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
      
      if (propertyInfo == null) {
        //If the type is of IEnumerable<T> get the properties from <T>
        if (instanceType.GetInterfaces().Contains(typeof(IEnumerable)))
        {
          Type enumerableType = instanceType.GetGenericArguments()[0];
          propertyInfo = GetProperty(enumerableType, propertyName);
          Type propertyEnumerable = typeof(IEnumerable<>).MakeGenericType(propertyInfo.PropertyType);
          propertyInfo = new SimplePropertyInfo(propertyName, propertyEnumerable);
        }
        else
        {
          var msg = String.Format("Unable to locate  '{0}' on type '{1}'.", propertyName, instanceType);
          throw new Exception(msg);
        }
      }
      return propertyInfo;
    }

    public Expression BuildMemberExpression(ParameterExpression parmExpr) {
      var prevProperty = Properties.First();
      Expression memberExpr = BuildPropertyExpression(parmExpr, prevProperty);

      foreach (var property in Properties.Skip(1))
      {          
        memberExpr = BuildPropertyExpression(memberExpr, property, prevProperty);
        prevProperty = property;
      }

      return memberExpr;
    }

    public Expression BuildPropertyExpression(Expression baseExpr, PropertyInfo property, PropertyInfo prevProperty = null) {
      var enumType = typeof(IEnumerable);
      if (prevProperty != null && prevProperty.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
      {
        return BuildSelectExpression(baseExpr, property, prevProperty);
      }
      return Expression.Property(baseExpr, property);
    }

    public Expression BuildSelectExpression(Expression baseExpr, PropertyInfo property, PropertyInfo prevProperty) {
      Type propertyEnumerableType = property.PropertyType.GetGenericArguments()[0];
      Type prevPropertyEnumerableType = prevProperty.PropertyType.GetGenericArguments()[0];

      MethodInfo selectMethod = null;

      foreach (MethodInfo m in typeof(Enumerable).GetMethods().Where(m => m.Name == "Select"))
      {
        foreach (ParameterInfo p in m.GetParameters().Where(p => p.Name.Equals("selector")))
        {
          if (p.ParameterType.GetGenericArguments().Count() == 2)
          {
            selectMethod = (MethodInfo)p.Member;
          }
        }
      }

      var prop = (PropertyInfo)TypeFns.FindPropertyOrField(prevPropertyEnumerableType, property.Name,
      BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

      var paramExpr = Expression.Parameter(prevPropertyEnumerableType, "t");

      var newExpr = Expression.Property(paramExpr, prop);

      var newLambda = Expression.Lambda(newExpr, paramExpr);

      return Expression.Call(
        null,
        selectMethod.MakeGenericMethod(new Type[] { prevPropertyEnumerableType, propertyEnumerableType }),
        new Expression[] {
          baseExpr, 
          newLambda
        }
      );
    }
    
    
  }

 
}
