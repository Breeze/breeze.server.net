// Only one of the next 3 should be uncommented.
#define CODEFIRST_PROVIDER 
//#define DATABASEFIRST_NEW
//#define NHIBERNATE

using System.Web.Http;
// using System.Web.Mvc;
using System.Web.Routing;

#if CODEFIRST_PROVIDER
using Models.NorthwindIB.CF;
using Foo;
using System.ComponentModel.DataAnnotations;
#elif DATABASEFIRST_NEW
using Models.NorthwindIB.EDMX_2012;
#endif

namespace Sample_WebApi2 {
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class WebApiApplication : System.Web.HttpApplication {
    protected void Application_Start() {

      // standard Breeze routing
      var routes = GlobalConfiguration.Configuration.Routes;
      routes.MapHttpRoute(
           name: "SampleApi",
           routeTemplate: "breezeTests/breeze/{controller}/{action}"
      );

      routes.MapHttpRoute(
          name: "config",
          routeTemplate: "api/testconfig",
          defaults: new { controller = "testconfig" }
      );


    }
  }

}