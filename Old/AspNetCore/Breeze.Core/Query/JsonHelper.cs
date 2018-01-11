using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Breeze.Core {


  public static class JsonHelper {
    public static object Deserialize(string json) {
      return ToObject(JToken.Parse(json));
    }

    private static object ToObject(JToken token) {
      switch (token.Type) {
        case JTokenType.Object:
          return token.Children<JProperty>()
                      .ToDictionary(prop => prop.Name,
                                    prop => ToObject(prop.Value));

        case JTokenType.Array:
          return token.Select(ToObject).ToList();

        default:
          return ((JValue)token).Value;
      }
    }
  }
}
