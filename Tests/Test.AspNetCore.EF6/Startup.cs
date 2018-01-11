using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Breeze.AspNetCore;
using Breeze.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models.NorthwindIB.CF;
using Newtonsoft.Json.Serialization;

namespace Test.AspNetCore {
  public class Startup {
    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {

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

      var tmp = Configuration.GetConnectionString("NorthwindIB_CF");
      services.AddScoped<NorthwindIBContext_CF>(_ => {
        return new NorthwindIBContext_CF(tmp);
      });

      //var tmp = Configuration.GetConnectionString("NorthwindIB_CF");
      //services.AddDbContext<NorthwindIBContext_CF>(options => options.UseSqlServer(tmp));
    }



    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      // allows use of html startup file.
      // app.UseStaticFiles();
      app.UseStaticFiles(new StaticFileOptions() {
        FileProvider = new PhysicalFileProvider(
           Path.Combine(Directory.GetCurrentDirectory(), @"breezeTests")),
        RequestPath = new PathString("")
      });

      app.UseMvc();
      
    }

    // old code
    //public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
    //  loggerFactory.AddConsole(Configuration.GetSection("Logging"));
    //  loggerFactory.AddDebug();

      
    //  // allows use of html startup file.
    //  // app.UseStaticFiles();
    //  app.UseStaticFiles(new StaticFileOptions() {
    //    FileProvider = new PhysicalFileProvider(
    //       Path.Combine(Directory.GetCurrentDirectory(), @"Tests")),
    //    RequestPath = new PathString("")
    //  });

    //  app.UseMvc();

    //  //app.UseExceptionHandler(errorApp => {
    //  //  errorApp.Run(async context => {
    //  //    context.Response.StatusCode = 500; // or another Status accordingly to Exception Type
    //  //    context.Response.ContentType = "application/json";

    //  //    var error = context.Features.Get<IExceptionHandlerFeature>();
    //  //    if (error != null) {
    //  //      var ex = error.Error;
    //  //      var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
    //  //      await context.Response.WriteAsync(new ErrorDto() {
    //  //        Code = 123,
    //  //        Message = msg 
    //  //       // + other custom data
    //  //      }.ToString(), Encoding.UTF8);
    //  //    }
    //  //  });
    //  //});
    //}

  }
}
