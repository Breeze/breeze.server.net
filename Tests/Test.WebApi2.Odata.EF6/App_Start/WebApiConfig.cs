// Only one of the next 4 should be uncommented.
#define CODEFIRST_PROVIDER
//#define DATABASEFIRST_OLD
//#define DATABASEFIRST_NEW
//#define NHIBERNATE


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Builder;
using Microsoft.Data.Edm;
using System.Web.Http.OData.Batch;

#if CODEFIRST_PROVIDER
using Models.NorthwindIB.CF;
using Foo;
using System.ComponentModel.DataAnnotations;
using System.Web.Http.OData.Batch;
using Microsoft.Data.Edm;
#elif DATABASEFIRST_NEW
using Models.NorthwindIB.EDMX_2012;
#endif

using Inheritance.Models;

using ProduceTPH;


namespace Foo {
    internal class NorthwindIBContext_Aliased : Models.NorthwindIB.CF.NorthwindIBContext_CF {}
}


namespace Test.WebApi2.OData {
  public static class WebApiConfig {
    public static void Register(HttpConfiguration config) {

#if !ODATA_MODEL_BUILDER

      config.Routes.MapODataRoute(
              routeName: "NorthwindIB_ODATA",
              routePrefix: "NorthwindIB_odata",
              model: EdmBuilder.GetEdm<NorthwindIBContext_Aliased>(),
              batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
              );
      
      var billingModel = EdmBuilder.GetEdm<InheritanceContext>();
      config.Routes.MapODataRoute(
        routeName: "BillingInheritance_ODATA",
        routePrefix: "BillingInheritance_odata",
        model: billingModel,
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
        );

      var produceModel = EdmBuilder.GetEdm<ProduceTPHContext>();
      config.Routes.MapODataRoute(
        routeName: "ProduceInheritance_ODATA",
        routePrefix: "ProduceInheritance_odata",
        model: produceModel,
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
        );
#else 
      // If using ODataConventionModelBuilder
      var builder = new ODataConventionModelBuilder();
      builder.EntitySet<Product>("Products");
      builder.EntitySet<Customer>("Customers");
      builder.EntitySet<Employee>("Employees");
      builder.EntitySet<Order>("Orders");
      builder.EntitySet<OrderDetail>("OrderDetails");
      builder.EntitySet<Category>("Categories");
      builder.EntitySet<Supplier>("Suppliers");
      builder.EntitySet<Region>("Regions");
      builder.EntitySet<Territory>("Territories");
      builder.EntitySet<User>("Users");
      builder.EntitySet<InternationalOrder>("InternationalOrders");
      builder.EntitySet<TimeLimit>("TimeLimits");
      builder.EntitySet<TimeGroup>("TimeGroups");
      builder.EntitySet<EmployeeTerritory>("EmployeeTerritories");
      builder.EntitySet<UserRole>("UserRoles");
      builder.EntitySet<Comment>("Comments");
      builder.EntitySet<Role>("Roles");

      config.Routes.MapODataRoute(
        routeName: "NorthwindIB_ODATA",
        routePrefix: "NorthwindIB_odata",
        model: builder.GetEdmModel(),
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
      );

      builder = new ODataConventionModelBuilder();
      builder.EntitySet<BillingDetailTPC>("BillingDetailTPCs");
      builder.EntitySet<BillingDetailTPH>("BillingDetailTPHs");
      builder.EntitySet<BillingDetailTPT>("BillingDetailTPTs");
      builder.EntitySet<BankAccountTPC>("BankAccountTPCs");
      builder.EntitySet<BankAccountTPH>("BankAccountTPHs");
      builder.EntitySet<BankAccountTPT>("BankAccountTPTs");
      builder.EntitySet<AccountType>("AccountTypes");
      builder.EntitySet<DepositTPC>("DepositTPCs");
      builder.EntitySet<DepositTPH>("DepositTPHs");
      builder.EntitySet<DepositTPT>("DepositTPTs");
      config.Routes.MapODataRoute(
        routeName: "BillingInheritance_ODATA",
        routePrefix: "BillingInheritance_odata",
        model: builder.GetEdmModel(),
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
      );

      builder = new ODataConventionModelBuilder();
      builder.EntitySet<ItemOfProduce>("ItemsOfProduce");
      builder.EntitySet<Fruit>("Fruits");
      config.Routes.MapODataRoute(
        routeName: "ProduceInheritance_ODATA",
        routePrefix: "ProduceInheritance_odata",
        model: builder.GetEdmModel(),
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
      );
#endif

    }
  }
}
