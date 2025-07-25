
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Breeze.Core {
  public class DataType {
    private String _name;
    private Type _type;
    private static Dictionary<String, DataType> _nameMap = new Dictionary<String, DataType>();
    private static Dictionary<Type, DataType> _typeMap = new Dictionary<Type, DataType>();

    public static DataType Binary = new DataType("Binary");
    public static DataType Guid = new DataType("Guid", typeof(System.Guid));
    public static DataType String = new DataType("String", typeof(string));

    public static DataType DateTime = new DataType("DateTime", typeof(DateTime));
    public static DataType DateTimeOffset = new DataType("DateTimeOffset", typeof(DateTimeOffset));
    public static DataType Time = new DataType("Time", typeof(TimeSpan));

    public static DataType Byte = new DataType("Byte", typeof(byte));
    public static DataType Int16 = new DataType("Int16", typeof(short));
    public static DataType Int32 = new DataType("Int32", typeof(int));
    public static DataType Int64 = new DataType("Int64", typeof(long));
    public static DataType Boolean = new DataType("Boolean", typeof(bool));

    public static DataType Decimal = new DataType("Decimal", typeof(Decimal));
    public static DataType Double = new DataType("Double", typeof(Double));
    public static DataType Single = new DataType("Single", typeof(Single));
#if NET6_0_OR_GREATER
    public static DataType DateOnly = new DataType("DateOnly", typeof(DateOnly));
    public static DataType TimeOnly = new DataType("TimeOnly", typeof(TimeOnly));
#endif

    public DataType(String name) {
      _name = name;
      _nameMap[name] = this;
    }

    public DataType(String name, Type type) {
      _name = name;
      _type = type;
      _nameMap[name] = this;
      _typeMap[type] = this;
    }


    public String GetName() {
      return _name;
    }

    public Type GetUnderlyingType() {
      return _type;
    }

    public static DataType FromName(String name) {
      return _nameMap[name];
    }

    public static DataType FromType(Type type) {
      var nnType = TypeFns.GetNonNullableType(type);
      return _typeMap[nnType];
    }

    /// <summary> Convert list to IList of itemType.  Handles case where itemType is enum and/or nullable. </summary>
    public static IList CoerceList(IList list, Type itemType) {
      var listType = typeof(List<>).MakeGenericType(new[] { itemType });
      var newList = (IList)Activator.CreateInstance(listType);
      var et = TypeFns.GetNonNullableType(itemType);
      if (et.IsEnum) {
        foreach (var item in list) {
          var enumVal = item == null ? null : item is string ? Enum.Parse(et, (String)item) : Enum.ToObject(et, item);
          newList.Add(enumVal);
        }
      } else {
        var dataType = DataType.FromType(et);
        foreach (var item in list) {
          var itemVal = item == null ? null : CoerceData(item, dataType);
          newList.Add(itemVal);
        }
      }
      return newList;
    }

    // Can't use this safely because of missing support for optional parts.
    // private static DateFormat ISO8601_Format = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSZ");

    /// <summary> Convert value to an object of the dataType </summary>
    public static Object CoerceData(Object value, DataType dataType) {

      if (value == null || dataType == null || value.GetType() == dataType.GetUnderlyingType()) {
        return value;
      } else if (value is IList ilist) {
        // this occurs with an 'In' clause
        return CoerceList(ilist, dataType.GetUnderlyingType());
      } else if (dataType == DataType.Guid) {
        return System.Guid.Parse(value.ToString());
      } else if (dataType == DataType.DateTimeOffset && value is DateTime) {
        DateTimeOffset result = (DateTime)value;
        return result;
      } else if (dataType == DataType.Time && value is String) {
        return XmlConvert.ToTimeSpan((string)value);
#if NET6_0_OR_GREATER
      } else if (dataType == DataType.DateOnly && value is String) {
        return System.DateOnly.Parse((string)value);
      } else if (dataType == DataType.TimeOnly && value is String) {
        return System.TimeOnly.Parse((string)value);
#endif
      } else {
        return Convert.ChangeType(value, dataType.GetUnderlyingType());
      }


    }
  }
}
