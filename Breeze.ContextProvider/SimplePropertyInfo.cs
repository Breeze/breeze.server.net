using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections;


namespace Breeze.ContextProvider
{


  internal class SimplePropertyInfo : PropertyInfo
  {
    private readonly string _name;
    private readonly Type _propertyType;

    public SimplePropertyInfo(string name, Type propertyType)
    {
      if (name == null)
        throw new ArgumentNullException("name");
      if (propertyType == null)
        throw new ArgumentNullException("propertyType");
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException("name");

      _name = name;
      _propertyType = propertyType;
    }

    public override string Name
    {
      get
      {
        return _name;
      }
    }

    public override Type PropertyType
    {
      get
      {
        return _propertyType;
      }
    }

    public override PropertyAttributes Attributes
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public override bool CanRead
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public override bool CanWrite
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public override Type DeclaringType
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public override Type ReflectedType
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public override MethodInfo[] GetAccessors(bool nonPublic)
    {
      throw new NotImplementedException();
    }

    public override MethodInfo GetGetMethod(bool nonPublic)
    {
      throw new NotImplementedException();
    }

    public override ParameterInfo[] GetIndexParameters()
    {
      throw new NotImplementedException();
    }

    public override MethodInfo GetSetMethod(bool nonPublic)
    {
      throw new NotImplementedException();
    }

    public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
      throw new NotImplementedException();
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
      throw new NotImplementedException();
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
      throw new NotImplementedException();
    }
  }

}
