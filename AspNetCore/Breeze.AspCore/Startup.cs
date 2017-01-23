using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.NorthwindIB.CF;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using Breeze.AspCore.Controllers;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;
using System.Xml;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Breeze.AspCore {
  public class Startup {
    public Startup(IHostingEnvironment env) {
      var builder = new ConfigurationBuilder()
          .SetBasePath(env.ContentRootPath)
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

      if (env.IsEnvironment("Development")) {
        // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
        builder.AddApplicationInsightsSettings(developerMode: true);
      }

      builder.AddEnvironmentVariables();
      Configuration = builder.Build();
    }

    public IConfigurationRoot Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services) {
      // Add framework services.
      services.AddApplicationInsightsTelemetry(Configuration);

      services.AddMvc().AddJsonOptions(opt => {
        var ss = opt.SerializerSettings;
        ss.NullValueHandling = NullValueHandling.Include;
        ss.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
        ss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        ss.TypeNameHandling = TypeNameHandling.Objects;
        ss.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
        ss.Converters.Add(new IsoDateTimeConverter {
          DateTimeFormat = "yyyy-MM-dd\\THH:mm:ss.fffK"
          // DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"
        });
        // Needed because JSON.NET does not natively support I8601 Duration formats for TimeSpan
        ss.Converters.Add(new TimeSpanConverter());
        ss.Converters.Add(new StringEnumConverter());

        var resolver = ss.ContractResolver;
        if (resolver != null) {
          var res = resolver as DefaultContractResolver;
          res.NamingStrategy = null;  // <<!-- this removes the camelcasing
        }

      });


      var connString = Configuration.GetConnectionString("NorthwindIB_CF");
      services.AddScoped<NorthwindIBContext_CF>(_ => {
        return new NorthwindIBContext_CF(connString);
      });



    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
      loggerFactory.AddConsole(Configuration.GetSection("Logging"));
      loggerFactory.AddDebug();

      app.UseApplicationInsightsRequestTelemetry();

      app.UseApplicationInsightsExceptionTelemetry();

      // allows use of html startup file.
      // app.UseStaticFiles();
      app.UseStaticFiles(new StaticFileOptions() {
        FileProvider = new PhysicalFileProvider(
           Path.Combine(Directory.GetCurrentDirectory(), @"Tests")),
        RequestPath = new PathString("")
      });


      app.UseMvc();
    }





  }


  public class DbConfig : DbConfiguration {
    public DbConfig() {
      SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
    }
  }

  // http://www.w3.org/TR/xmlschema-2/#duration
  public class TimeSpanConverter : JsonConverter {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      var ts = (TimeSpan)value;
      var tsString = XmlConvert.ToString(ts);
      serializer.Serialize(writer, tsString);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      if (reader.TokenType == JsonToken.Null) {
        return null;
      }

      var value = serializer.Deserialize<String>(reader);
      return XmlConvert.ToTimeSpan(value);
    }

    public override bool CanConvert(Type objectType) {
      return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
    }
  }
}

