// Only one of the next 4 should be uncommented.
#define CODEFIRST_PROVIDER
//#define DATABASEFIRST_OLD
//#define DATABASEFIRST_NEW
//#define NHIBERNATE


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
// using Microsoft.Data.Edm;

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


      // If using ODataConventionModelBuilder
      var builder = new ODataConventionModelBuilder();
      
      var productType = builder.EntityType<Product>();
      builder.EntitySet<Product>("Products");
      builder.EntitySet<Customer>("Customers");
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


    }
  }
}
