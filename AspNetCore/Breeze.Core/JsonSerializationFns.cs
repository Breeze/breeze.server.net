using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization.Formatters;

namespace Breeze.Core {
  public static class JsonSerializationFns {

    public static JsonSerializerSettings UpdateWithDefaults(JsonSerializerSettings ss) {
      ss.NullValueHandling = NullValueHandling.Include;
      ss.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
      ss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
      ss.TypeNameHandling = TypeNameHandling.Objects;
      // ss.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
      ss.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;

      // Hack is for the issue described in this post:
      // http://stackoverflow.com/questions/11789114/internet-explorer-json-net-javascript-date-and-milliseconds-issue
      ss.Converters.Add(new IsoDateTimeConverter {
        DateTimeFormat = "yyyy-MM-dd\\THH:mm:ss.fffK"
        // DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"
      });

      // Needed because JSON.NET does not natively support I8601 Duration formats for TimeSpan
      ss.Converters.Add(new TimeSpanConverter());
      ss.Converters.Add(new StringEnumConverter());
      // only needed because this functionality seems to be broken in JSON.NET 10.0.3
      ss.Converters.Add(new ByteArrayConverter());

      // Default is DateTimeZoneHandling.RoundtripKind - you can change that here.
      // ss.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

      return ss;
    }
  }
}
