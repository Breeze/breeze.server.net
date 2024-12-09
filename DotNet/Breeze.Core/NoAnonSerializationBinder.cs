using Newtonsoft.Json.Serialization;
using System;

namespace Breeze.Core {

  /// <summary> Serialization binder for anonymous types, such as query projections.  
  /// Removes the verbose type name from the serialized JSON to reduce payload size. </summary>
  public class NoAnonSerializationBinder : DefaultSerializationBinder {

    /// <summary> Don't return assemblyName or typeName if anonymous type </summary>
    public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
      base.BindToName(serializedType, out assemblyName, out typeName);
      if (typeName != null && (typeName.StartsWith("<>") || typeName.StartsWith("_IB_") || typeName.StartsWith("VB$"))) {
        assemblyName = null;
        typeName = null;
      }
    }
  }
}
