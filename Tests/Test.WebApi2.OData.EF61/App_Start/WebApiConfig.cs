using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

using System.Web.Http.OData.Builder;
using Foo;
using Microsoft.Data.Edm;
using System.Web.Http.OData.Batch;
using Models.NorthwindIB.CF;

namespace Test.WebApi2.OData.EF61
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config) {

        config.Routes.MapODataRoute(
            routeName: "odata",
            routePrefix: "odata",
            model: EdmBuilder.GetEdm<NorthwindIBContext_CF>(),
            batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
            );

        //ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
        //builder.EntitySet<Product>("Products");
        //builder.EntitySet<Customer>("Customers");
        //builder.EntitySet<Order>("Order"); 
        //builder.EntitySet<Category>("Categories");
        //builder.EntitySet<Supplier>("Suppliers"); 
        //config.Routes.MapODataRoute("odata", "odata", builder.GetEdmModel());

            //// Web API configuration and services

            //// Web API routes
            //config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);
        }
    }
}
