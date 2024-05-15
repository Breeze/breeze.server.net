using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Breeze.Core {
  /// <summary> Static functions related to JSON serialization </summary>
  public static class JsonSerializationFns {

    /// <summary> Set the NewtonSoftJson settings for Breeze serialization of entities.
    /// This ensures that the Breeze client can correctly identify and parse entities from the server. </summary>
    /// <param name="ss">Initial settings, see example below.</param>
    /// <param name="camelCasing">Whether to camelCase property names.  Default is false because this is normally handled on the client. </param>
    /// <param name="useIntEnums">Whether to pass enum values as int instead of string.  Default is false (string) for backward compatibility.
    /// See <see href="https://github.com/Breeze/breeze.server.net/issues/196" />
    /// </param>
    /// <example><code>
    /// services.AddControllers().AddNewtonsoftJson(opt => {
    ///   var ss = JsonSerializationFns.UpdateWithDefaults(opt.SerializerSettings, false, BreezeConfig.Instance.UseIntEnums);
    ///   ss.SerializationBinder = new NoAnonSerializationBinder();
    /// }
    /// </code></example>
    public static JsonSerializerSettings UpdateWithDefaults(JsonSerializerSettings ss, bool camelCasing = false, bool useIntEnums = false) {
      ss.NullValueHandling = NullValueHandling.Include;
      ss.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
      ss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
      ss.TypeNameHandling = TypeNameHandling.Objects;
      // ss.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
      ss.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;

      // DateTime format is:  yyyy-MM-ddTHH:mm:ss.fff
      // DateTime2 format is: yyyy-MM-ddTHH:mm:ss.fffffffK

      // Hack is for the issue described in this post:
      // http://stackoverflow.com/questions/11789114/internet-explorer-json-net-javascript-date-and-milliseconds-issue
      // See also: https://stackoverflow.com/questions/52048935/how-do-i-get-entityframeworkcore-generated-sql-to-use-the-right-format-for-datet
      ss.Converters.Add(new IsoDateTimeConverter {
        // datetime2
        DateTimeFormat = "yyyy-MM-dd\\THH:mm:ss.fffffffK"
        // datetime
        // DateTimeFormat = "yyyy-MM-dd\\THH:mm:ss.fffK"
        // old 
        // DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"
      });


      // Needed because JSON.NET does not natively support I8601 Duration formats for TimeSpan
      ss.Converters.Add(new TimeSpanConverter());
      if (!useIntEnums) {
        ss.Converters.Add(new StringEnumConverter());
      }
      // only needed because this functionality seems to be broken in JSON.NET 10.0.3
      ss.Converters.Add(new ByteArrayConverter());

      if (!camelCasing) {
        if (ss.ContractResolver is DefaultContractResolver resolver) {
          resolver.NamingStrategy = null;  // remove json camelCasing; names are converted on the client.
        }
      }

      // Default is DateTimeZoneHandling.RoundtripKind - you can change that here.
      // ss.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

      return ss;
    }
  }
}
