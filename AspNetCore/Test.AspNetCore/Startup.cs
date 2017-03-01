﻿using Breeze.AspNetCore;
using Breeze.Core;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Models.NorthwindIB.CF;
using Newtonsoft.Json.Serialization;
using ProduceTPH;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.IO;

namespace Test.AspNetCore {
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

      var mvcBuilder = services.AddMvc();
      
      
      mvcBuilder.AddJsonOptions(opt => {
        var ss = JsonSerializationFns.UpdateWithDefaults(opt.SerializerSettings);
        var resolver = ss.ContractResolver;
        if (resolver != null) {
          var res = resolver as DefaultContractResolver;
          res.NamingStrategy = null;  // <<!-- this removes the camelcasing
        }

      });

      
      mvcBuilder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter()); });

      var northwindIBConnStr = Configuration.GetConnectionString("NorthwindIB_CF");
      var produceTPHConnStr = Configuration.GetConnectionString("ProduceTPH");
      services.AddScoped<NorthwindIBContext_CF>(_ => {
        return new NorthwindIBContext_CF(northwindIBConnStr);
      });
      services.AddScoped<ProduceTPHContext>(_ => {
        return new ProduceTPHContext(produceTPHConnStr);
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

      //app.UseExceptionHandler(errorApp => {
      //  errorApp.Run(async context => {
      //    context.Response.StatusCode = 500; // or another Status accordingly to Exception Type
      //    context.Response.ContentType = "application/json";

      //    var error = context.Features.Get<IExceptionHandlerFeature>();
      //    if (error != null) {
      //      var ex = error.Error;
      //      var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
      //      await context.Response.WriteAsync(new ErrorDto() {
      //        Code = 123,
      //        Message = msg 
      //       // + other custom data
      //      }.ToString(), Encoding.UTF8);
      //    }
      //  });
      //});
    }

  }


  public class DbConfig : DbConfiguration {
    public DbConfig() {
      SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
    }
  }

  
}

