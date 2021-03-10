// Uncomment only one of the two defines below
#define EFCORE
// #define NHIBERNATE

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
using Models.NorthwindIB.CF;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test.AspNetCore5.EFCore5 {
  public class Startup {

    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }
    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940


    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      services.AddMvc(option => option.EnableEndpointRouting = false);
      var mvcBuilder = services.AddMvc();
      services.AddControllers().AddNewtonsoftJson();
      services.AddControllers().AddNewtonsoftJson(opt => {
        var ss = JsonSerializationFns.UpdateWithDefaults(opt.SerializerSettings);
        var resolver = ss.ContractResolver;
        if (resolver != null) {
          var res = resolver as DefaultContractResolver;
          res.NamingStrategy = null;  // <<!-- this removes the camelcasing
        }

#if NHIBERNATE
        // NHibernate settings
        var settings = opt.SerializerSettings;
        settings.ContractResolver = NHibernateContractResolver.Instance;

        settings.Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) {
          // When the NHibernate session is closed, NH proxies throw LazyInitializationException when
          // the serializer tries to access them.  We want to ignore those exceptions.
          var error = args.ErrorContext.Error;
          if (error is NHibernate.LazyInitializationException || error is System.ObjectDisposedException)
            args.ErrorContext.Handled = true;
        };

        if (!settings.Converters.Any(c => c is NHibernateProxyJsonConverter)) {
          settings.Converters.Add(new NHibernateProxyJsonConverter());
        }
#endif

      });


      mvcBuilder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter()); });

      var tmp = Configuration.GetConnectionString("NorthwindIB_CF");
#if EFCORE
      services.AddDbContext<NorthwindIBContext_CF>(options => options.UseSqlServer(tmp));
#endif

#if NHIBERNATE
      services.AddSingleton<NHibernate.ISessionFactory>(factory => {
        var cfg = new NHibernate.Cfg.Configuration();
        cfg.DataBaseIntegration(db => {
          db.ConnectionString = tmp;
          db.Dialect<MsSql2008Dialect>();
          db.Driver<Sql2008ClientDriver>();
          db.LogFormattedSql = true;
          db.LogSqlInConsole = true;
        });
        cfg.Properties.Add(Environment.DefaultBatchFetchSize, "32");
        cfg.CurrentSessionContext<NHibernate.Context.ThreadStaticSessionContext>();
        var modelAssembly = typeof(Models.NorthwindIB.NH.Customer).Assembly;
        cfg.AddAssembly(modelAssembly);  // mapping is in this assembly

        var sessionFactory = cfg.BuildSessionFactory();
        return sessionFactory;
      });

#endif
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      // allows use of html startup file.
      // This code assumes that the 'breezeTests' dir has all of the breeze test files and a copy of the breeze.debug.js file in a breeze subdir.
      var path = Path.Combine(Directory.GetCurrentDirectory(), @"breezeTests");
      app.UseStaticFiles(new StaticFileOptions() {
        FileProvider = new PhysicalFileProvider(path),
        RequestPath = new PathString("")
      });

      app.UseMvc();

    }
    //public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
    //    if (env.IsDevelopment()) {
    //        app.UseDeveloperExceptionPage();
    //    }

    //    app.UseRouting();

    //    app.UseEndpoints(endpoints =>
    //    {
    //        endpoints.MapGet("/", async context =>
    //        {
    //            await context.Response.WriteAsync("Hello World!");
    //        });
    //    });
    //}


  }
}
