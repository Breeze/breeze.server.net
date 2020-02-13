using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace Test.WebApi2.OData4 {
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class WebApiApplication : System.Web.HttpApplication {
    protected void Application_Start() {

      GlobalConfiguration.Configure(WebApiConfig.Register);
     
    }



  }

}