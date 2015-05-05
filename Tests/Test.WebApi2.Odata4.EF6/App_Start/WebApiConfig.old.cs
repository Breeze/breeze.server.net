// Only one of the next 4 should be uncommented.
#define CODEFIRST_PROVIDER
//#define DATABASEFIRST_OLD
//#define DATABASEFIRST_NEW
//#define NHIBERNATE


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
// using Microsoft.Data.Edm;
using System.Web.OData.Batch;

#if CODEFIRST_PROVIDER
using Models.NorthwindIB.CF;
using Foo;
using System.ComponentModel.DataAnnotations;
// using Microsoft.Data.Edm;
#elif DATABASEFIRST_NEW
using Models.NorthwindIB.EDMX_2012;
#endif

using Inheritance.Models;

using ProduceTPH;


namespace Foo {
    internal class NorthwindIBContext_Aliased : Models.NorthwindIB.CF.NorthwindIBContext_CF {}
}


namespace Test.WebApi2.OData4 {
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
      
      var productType = builder.EntityType<Product>();
      productType.Ignore(t => t.DiscontinuedDate);
      productType.Property(t => t.OData4DiscontinuedDate).Name = "DiscontinuedDate";
      builder.EntitySet<Product>("Products");

      
      builder.EntitySet<Customer>("Customers");

      var empType = builder.EntityType<Employee>();
      empType.Ignore(t => t.BirthDate);
      empType.Property(t => t.OData4BirthDate).Name = "BirthDate";
      empType.Ignore(t => t.HireDate);
      empType.Property(t => t.OData4HireDate).Name = "HireDate";
      //var x = empType.HasMany(t => t.Orders).
      //builder.EntitySet<Employee>("Employees");

      var orderType = builder.EntityType<Order>();
      orderType.Ignore(t => t.OrderDate);
      orderType.Property(t => t.OData4OrderDate).Name = "OrderDate";
      orderType.Ignore(t => t.RequiredDate);
      orderType.Property(t => t.OData4RequiredDate).Name = "RequiredDate";
      orderType.Ignore(t => t.ShippedDate);
      orderType.Property(t => t.OData4ShippedDate).Name = "ShippedDate";
      builder.EntitySet<Order>("Orders");

      builder.EntitySet<OrderDetail>("OrderDetails");
      builder.EntitySet<Category>("Categories");
      builder.EntitySet<Supplier>("Suppliers");
      builder.EntitySet<Region>("Regions");
      builder.EntitySet<Territory>("Territories");
      
      var userType = builder.EntityType<User>();
      userType.Ignore(t => t.CreatedDate);
      userType.Property(t => t.OData4CreatedDate).Name = "CreatedDate";
      userType.Ignore(t => t.ModifiedDate);
      userType.Property(t => t.OData4ModifiedDate).Name = "ModifiedDate";
      builder.EntitySet<User>("Users");

      builder.EntitySet<InternationalOrder>("InternationalOrders");
      builder.EntitySet<TimeLimit>("TimeLimits");
      builder.EntitySet<TimeGroup>("TimeGroups");
      builder.EntitySet<EmployeeTerritory>("EmployeeTerritories");
      builder.EntitySet<UserRole>("UserRoles");

      var commentType = builder.EntityType<Comment>();
      commentType.Ignore(t => t.CreatedOn);
      commentType.Property(t => t.OData4CreatedOn).Name = "CreatedOn";
      builder.EntitySet<Comment>("Comments");

      // builder.EntitySet<Role>("Roles");

      config.MapODataServiceRoute(
        routeName: "NorthwindIB_ODATA",
        routePrefix: "NorthwindIB_odata",
        model: builder.GetEdmModel(),
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
      );

      //builder = new ODataConventionModelBuilder();

      ////var bdType = builder.EntityType<IBillingDetail>();
      ////bdType.Ignore(t => t.CreatedAt);
      ////builder.EntitySet<IBillingDetail>("IBillingDetails");
      
      //// BillingDetail
      //var bdTPCType = builder.EntityType<BillingDetailTPC>();
      //bdTPCType.Ignore(t => t.CreatedAt);
      //bdTPCType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<BillingDetailTPC>("BillingDetailTPCs");

      //var bdTPHType = builder.EntityType<BillingDetailTPH>();
      //bdTPHType.Ignore(t => t.CreatedAt);
      //bdTPHType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<BillingDetailTPH>("BillingDetailTPHs");

      //var bdTPTType = builder.EntityType<BillingDetailTPT>();
      //bdTPTType.Ignore(t => t.CreatedAt);
      //bdTPTType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<BillingDetailTPT>("BillingDetailTPTs");
      //// BankAccount
      //var baTPCType = builder.EntityType<BankAccountTPC>();
      //baTPCType.Ignore(t => t.CreatedAt);
      //baTPCType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<BankAccountTPC>("BankAccountTPCs");

      //var baTPHType = builder.EntityType<BankAccountTPH>();
      //baTPHType.Ignore(t => t.CreatedAt);
      //baTPHType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<BankAccountTPH>("BankAccountTPHs");

      //var baTPTType = builder.EntityType<BankAccountTPT>();
      //baTPTType.Ignore(t => t.CreatedAt);
      //baTPTType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<BankAccountTPT>("BankAccountTPTs");
      //// CreditCard
      //var ccTPCType = builder.EntityType<CreditCardTPC>();
      //ccTPCType.Ignore(t => t.CreatedAt);
      //ccTPCType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<CreditCardTPC>("CreditCardTPCs");

      //var ccTPHType = builder.EntityType<CreditCardTPH>();
      //ccTPHType.Ignore(t => t.CreatedAt);
      //ccTPHType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<CreditCardTPH>("CreditCardTPHs");

      //var ccTPTType = builder.EntityType<CreditCardTPT>();
      //ccTPTType.Ignore(t => t.CreatedAt);
      //ccTPTType.Property(t => t.OData4CreatedAt).Name = "CreatedAt";
      //builder.EntitySet<CreditCardTPT>("CreditCardTPTs");
      //// Deposit
      //var depositTPCType = builder.EntityType<DepositTPC>();
      //depositTPCType.Ignore(t => t.Deposited);
      //depositTPCType.Property(t => t.OData4Deposited).Name = "Deposited";
      //depositTPCType.HasRequired(t => t.BankAccount).AddedExplicitly = true;
      //builder.EntitySet<DepositTPC>("DepositTPCs");

      //var depositTPHType = builder.EntityType<DepositTPH>();
      //depositTPHType.Ignore(t => t.Deposited);
      //depositTPHType.Property(t => t.OData4Deposited).Name = "Deposited";

      //builder.EntitySet<DepositTPH>("DepositTPHs");

      //var depositTPTType = builder.EntityType<DepositTPT>();
      //depositTPTType.Ignore(t => t.Deposited);
      //depositTPTType.Property(t => t.OData4Deposited).Name = "Deposited";
      //builder.EntitySet<DepositTPT>("DepositTPTs");
      //// AccountType
      //builder.EntitySet<AccountType>("AccountTypes");

      //config.MapODataServiceRoute(
      //  routeName: "BillingInheritance_ODATA",
      //  routePrefix: "BillingInheritance_odata",
      //  model: builder.GetEdmModel(),
      //  batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
      //);

      builder = new ODataConventionModelBuilder();
      builder.EntitySet<ItemOfProduce>("ItemsOfProduce");
      builder.EntitySet<Fruit>("Fruits");
      config.MapODataServiceRoute(
        routeName: "ProduceInheritance_ODATA",
        routePrefix: "ProduceInheritance_odata",
        model: builder.GetEdmModel(),
        batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
      );
#endif

    }
  }
}
