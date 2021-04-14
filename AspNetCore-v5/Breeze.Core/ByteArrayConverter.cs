using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

// only needed because this functionality seems to be broken in JSON.NET 10.0.3
namespace Breeze.Core {
  public class ByteArrayConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
      return objectType == typeof(byte[]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      if (reader.TokenType == JsonToken.Null)
        return null;
      var token = JToken.Load(reader);
      if (token == null)
        return null;
      switch (token.Type) {
        case JTokenType.Null:
          return null;
        case JTokenType.String:
          return Convert.FromBase64String((string)token);
        case JTokenType.Object: {
            var value = (string)token["$value"];
            return value == null ? null : Convert.FromBase64String(value);
          }
        default:
          throw new JsonSerializationException("Unknown byte array format");
      }
    }

    public override bool CanWrite { get { return true; } }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      string base64String = Convert.ToBase64String((byte[])value);

      serializer.Serialize(writer, base64String);
    }
  }


}
