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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
      services.AddMvc(option => option.EnableEndpointRouting = false);
      var mvcBuilder = services.AddMvc();
      services.AddControllers().AddNewtonsoftJson();
      services.AddControllers().AddNewtonsoftJson(opt =>       {
        var ss = JsonSerializationFns.UpdateWithDefaults(opt.SerializerSettings);
        var resolver = ss.ContractResolver;
        if (resolver != null) {
          var res = resolver as DefaultContractResolver;
          res.NamingStrategy = null;  // <<!-- this removes the camelcasing
        }
      });

      mvcBuilder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter()); });

      var tmp = Configuration.GetConnectionString("NorthwindIB_CF");
      services.AddDbContext<NorthwindIBContext_CF>(options => options.UseSqlServer(tmp));
    }



    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      // allows use of html startup file.
      // app.UseStaticFiles();
      var path = Path.Combine(Directory.GetCurrentDirectory(), @"breezeTests");
      app.UseStaticFiles(new StaticFileOptions()
      {
        FileProvider = new PhysicalFileProvider(path),
        RequestPath = new PathString("")
      });
      
      app.UseMvc();
      

    }



  }
}
