﻿// Only one of the next few should be uncommented.
#define CODEFIRST_PROVIDER
//#define DATABASEFIRST_NEW
//#define ORACLE_EDMX
//#define NHIBERNATE

// breeze/NorthwindIBModel/customers

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

using Breeze.Persistence;
using Foo;
using Models.NorthwindIB.CF;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using System.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using Breeze.AspNetCore;



#if CODEFIRST_PROVIDER
using Breeze.Persistence.EF6;
#elif DATABASEFIRST_NEW
using Breeze.Persistence.EF6;
using Models.NorthwindIB.EDMX_2012;
#endif

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class NorthwindIBModelController : Controller {
    private NorthwindPersistenceManager PersistenceManager;

    // called via DI 
    public NorthwindIBModelController(NorthwindIBContext_CF context) {
      PersistenceManager = new NorthwindPersistenceManager(context);
    }


    [HttpGet]
    public IActionResult Metadata() {
      return Ok(PersistenceManager.Metadata());
    }
    [HttpPost]
    public SaveResult SaveChanges([FromBody] JObject saveBundle) {
      return PersistenceManager.SaveChanges(saveBundle);
    }

    #region Save interceptors 
    [HttpPost]
    public SaveResult SaveWithTransactionScope([FromBody]JObject saveBundle) {
      var txSettings = new TransactionSettings() { TransactionType = TransactionType.TransactionScope };
      return PersistenceManager.SaveChanges(saveBundle, txSettings);
    }

    [HttpPost]
    public SaveResult SaveWithDbTransaction([FromBody]JObject saveBundle) {
      var txSettings = new TransactionSettings() { TransactionType = TransactionType.DbTransaction };
      return PersistenceManager.SaveChanges(saveBundle, txSettings);
    }

    [HttpPost]
    public SaveResult SaveWithNoTransaction([FromBody]JObject saveBundle) {
      var txSettings = new TransactionSettings() { TransactionType = TransactionType.None };
      return PersistenceManager.SaveChanges(saveBundle, txSettings);
    }

    [HttpPost]
    public SaveResult SaveWithComment([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntitiesDelegate = AddComment;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveWithExit([FromBody]JObject saveBundle) {
      // set break points here to see how these two approaches give you a SaveMap w/o saving.
      var saveMap = PersistenceManager.GetSaveMapFromSaveBundle(saveBundle);
      saveMap = new NorthwindIBDoNotSaveContext().GetSaveMapFromSaveBundle(saveBundle);
      return new SaveResult() { Entities = new List<Object>(), KeyMappings = new List<KeyMapping>() };
    }

    [HttpPost]
    public SaveResult SaveAndThrow([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntitiesDelegate = ThrowError;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveWithEntityErrorsException([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntitiesDelegate = ThrowEntityErrorsException;
      return PersistenceManager.SaveChanges(saveBundle);
    }


    [HttpPost]
    public SaveResult SaveWithFreight([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntityDelegate = CheckFreight;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveWithFreight2([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntitiesDelegate = CheckFreightOnOrders;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckInitializer([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntitiesDelegate = AddOrder;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckUnmappedProperty([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntityDelegate = CheckUnmappedProperty;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckUnmappedPropertySerialized([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntityDelegate = CheckUnmappedPropertySerialized;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckUnmappedPropertySuppressed([FromBody]JObject saveBundle) {
      PersistenceManager.BeforeSaveEntityDelegate = CheckUnmappedPropertySuppressed;
      return PersistenceManager.SaveChanges(saveBundle);
    }

    private Dictionary<Type, List<EntityInfo>> ThrowError(Dictionary<Type, List<EntityInfo>> saveMap) {
      throw new Exception("Deliberately thrown exception");
    }

    private Dictionary<Type, List<EntityInfo>> ThrowEntityErrorsException(Dictionary<Type, List<EntityInfo>> saveMap) {
      List<EntityInfo> orderInfos;
      if (saveMap.TryGetValue(typeof(Order), out orderInfos)) {
        var errors = orderInfos.Select(oi => {
#if NHIBERNATE
          return new EntityError() {
            EntityTypeName = typeof(Order).FullName,
            ErrorMessage = "Cannot save orders with this save method",
            ErrorName = "WrongMethod",
            KeyValues = new object[] { ((Order) oi.Entity).OrderID },
            PropertyName = "OrderID"
          };
#else
          return new EFEntityError(oi, "WrongMethod", "Cannot save orders with this save method", "OrderID");
#endif
        });
        var ex = new EntityErrorsException("test of custom exception message", errors);
        // if you want to see a different error status code use this.
        // ex.StatusCode = HttpStatusCode.Conflict; // Conflict = 409 ; default is Forbidden (403).
        throw ex;
      }
      return saveMap;
    }

    private Dictionary<Type, List<EntityInfo>> AddOrder(Dictionary<Type, List<EntityInfo>> saveMap) {
      var order = new Order();
      order.OrderDate = DateTime.Today;
      var ei = PersistenceManager.CreateEntityInfo(order);
      List<EntityInfo> orderInfos = PersistenceManager.GetEntityInfos(saveMap, typeof(Order));
      orderInfos.Add(ei);

      return saveMap;
    }

    private Dictionary<Type, List<EntityInfo>> CheckFreightOnOrders(Dictionary<Type, List<EntityInfo>> saveMap) {
      List<EntityInfo> entityInfos;
      if (saveMap.TryGetValue(typeof(Order), out entityInfos)) {
        foreach (var entityInfo in entityInfos) {
          CheckFreight(entityInfo);
        }
      }

      return saveMap;
    }

    private bool CheckFreight(EntityInfo entityInfo) {
      if ((PersistenceManager.SaveOptions.Tag as String) == "freight update") {
        var order = entityInfo.Entity as Order;
        order.Freight = order.Freight + 1;
      } else if ((PersistenceManager.SaveOptions.Tag as String) == "freight update-ov") {
        var order = entityInfo.Entity as Order;
        order.Freight = order.Freight + 1;
        entityInfo.OriginalValuesMap["Freight"] = null;
      } else if ((PersistenceManager.SaveOptions.Tag as String) == "freight update-force") {
        var order = entityInfo.Entity as Order;
        order.Freight = order.Freight + 1;
        entityInfo.ForceUpdate = true;
      }
      return true;
    }

    private Dictionary<Type, List<EntityInfo>> AddComment(Dictionary<Type, List<EntityInfo>> saveMap) {
      var comment = new Comment();
      var tag = PersistenceManager.SaveOptions.Tag;
      comment.Comment1 = (tag == null) ? "Generic comment" : tag.ToString();
      comment.CreatedOn = DateTime.Now;
      comment.SeqNum = 1;
      var ei = PersistenceManager.CreateEntityInfo(comment);
      List<EntityInfo> commentInfos = PersistenceManager.GetEntityInfos(saveMap, typeof(Comment));
      commentInfos.Add(ei);

      return saveMap;
    }

    private bool CheckUnmappedProperty(EntityInfo entityInfo) {
      var unmappedValue = entityInfo.UnmappedValuesMap["MyUnmappedProperty"];
      // fixed in v 1.4.18
      // var unmappedValue = entityInfo.UnmappedValuesMap["myUnmappedProperty"];
      if ((String)unmappedValue != "anything22") {
        throw new Exception("wrong value for unmapped property:  " + unmappedValue);
      }
      Customer cust = entityInfo.Entity as Customer;
      return false;
    }

    private bool CheckUnmappedPropertySuppressed(EntityInfo entityInfo) {
      if (entityInfo.UnmappedValuesMap != null) {
        throw new Exception("unmapped properties should have been suppressed");
      }
      return false;
    }

    private bool CheckUnmappedPropertySerialized(EntityInfo entityInfo) {
      var unmappedValue = entityInfo.UnmappedValuesMap["MyUnmappedProperty"];
      if ((String)unmappedValue != "ANYTHING22") {
        throw new Exception("wrong value for unmapped property:  " + unmappedValue);
      }
      var anotherOne = entityInfo.UnmappedValuesMap["AnotherOne"];

      if (((dynamic)anotherOne).z[5].foo.Value != 4) {
        throw new Exception("wrong value for 'anotherOne.z[5].foo'");
      }

      if (((dynamic)anotherOne).extra.Value != 666) {
        throw new Exception("wrong value for 'anotherOne.extra'");
      }

      Customer cust = entityInfo.Entity as Customer;
      if (cust.CompanyName.ToUpper() != cust.CompanyName) {
        throw new Exception("Uppercasing of company name did not occur");
      }
      return false;
    }
    #endregion

    #region standard queries

    [HttpGet]
    //    [EnableBreezeQuery(MaxAnyAllExpressionDepth = 3)]
    public IQueryable<Customer> Customers() {
      var list = PersistenceManager.Context.Customers;
      return list;
    }

    [HttpGet]
    //    [EnableBreezeQuery(MaxExpansionDepth = 3)]
    public IQueryable<Order> Orders() {
      return PersistenceManager.Context.Orders;
    }

    [HttpGet]
    public IQueryable<Employee> Employees() {
      return PersistenceManager.Context.Employees;
    }

    [HttpGet]
    public IQueryable<OrderDetail> OrderDetails() {
      return PersistenceManager.Context.OrderDetails;
    }

    [HttpGet]
    public IQueryable<Product> Products() {
      return PersistenceManager.Context.Products;
    }

    [HttpGet]
    public IQueryable<Supplier> Suppliers() {
      return PersistenceManager.Context.Suppliers;
    }

    [HttpGet]
    public IQueryable<Region> Regions() {
      return PersistenceManager.Context.Regions;
    }

    [HttpGet]
    public IQueryable<Territory> Territories() {
      return PersistenceManager.Context.Territories;
    }

    [HttpGet]
    public IQueryable<Category> Categories() {
      return PersistenceManager.Context.Categories;
    }

    [HttpGet]
    public IQueryable<Role> Roles() {
      return PersistenceManager.Context.Roles;
    }

    [HttpGet]
    public IQueryable<User> Users() {
      return PersistenceManager.Context.Users;
    }

    [HttpGet]
    public IQueryable<TimeLimit> TimeLimits() {
      return PersistenceManager.Context.TimeLimits;
    }

    [HttpGet]
    public IQueryable<TimeGroup> TimeGroups() {
      return PersistenceManager.Context.TimeGroups;
    }

    [HttpGet]
    public IQueryable<Comment> Comments() {
      return PersistenceManager.Context.Comments;
    }

    [HttpGet]
    public IQueryable<UnusualDate> UnusualDates() {
      return PersistenceManager.Context.UnusualDates;
    }

#if !DATABASEFIRST_OLD
    [HttpGet]
    public IQueryable<Geospatial> Geospatials() {
      return PersistenceManager.Context.Geospatials;
    }
#endif

    #endregion

    #region named queries

    [HttpGet]
    public IQueryable<Customer> CustomersStartingWith([Required] string companyName) {
      if (companyName == "null") {
        throw new Exception("nulls should not be passed as 'null'");
      }
      if (String.IsNullOrEmpty(companyName)) {
        companyName = "";
      }
      var custs = PersistenceManager.Context.Customers.Where(c => c.CompanyName.StartsWith(companyName));
      return custs;
    }

    [HttpGet]
    public Object CustomerCountsByCountry() {
      return PersistenceManager.Context.Customers.GroupBy(c => c.Country).Select(g => new { g.Key, Count = g.Count() });
    }


    [HttpGet]
    public Customer CustomerWithScalarResult() {
      return PersistenceManager.Context.Customers.First();
    }

    [HttpGet]
    public IActionResult CustomersWithHttpError() {
      //var responseMsg = new HttpResponseMessage(HttpStatusCode.NotFound);
      //responseMsg.Content = new StringContent("Custom error message");
      //responseMsg.ReasonPhrase = "Custom Reason";
      //throw new HttpResponseException(responseMsg);
      return StatusCode(StatusCodes.Status404NotFound, "Custom error message");
    }

    [HttpGet]
    // [EnableBreezeQuery]
    public IEnumerable<Employee> EnumerableEmployees() {
      return PersistenceManager.Context.Employees.ToList();
    }

    [HttpGet]
    public IQueryable<Employee> EmployeesFilteredByCountryAndBirthdate(DateTime birthDate, string country) {
      return PersistenceManager.Context.Employees.Where(emp => emp.BirthDate >= birthDate && emp.Country == country);
    }

    [HttpGet]
    public List<Employee> QueryInvolvingMultipleEntities() {
#if NHIBERNATE
        // need to figure out what to do here
        //return new List<Employee>();
        var dc0 = new NorthwindNHContext();
        var dc = new NorthwindNHContext();
#elif CODEFIRST_PROVIDER
      var dc0 = new NorthwindIBContext_CF();
      var dc = new EFPersistenceManager<NorthwindIBContext_CF>();
#elif DATABASEFIRST_OLD
        var dc0 = new NorthwindIBContext_EDMX();
        var dc = new EFContextProvider<NorthwindIBContext_EDMX>();
#elif DATABASEFIRST_NEW
      var dc0 = new NorthwindIBContext_EDMX_2012();
      var dc = new EFContextProvider<NorthwindIBContext_EDMX_2012>();
#elif ORACLE_EDMX
      var dc0 = new NorthwindIBContext_EDMX_Oracle();
      var dc = new EFContextProvider<NorthwindIBContext_EDMX_Oracle>();
#endif
      //the query executes using pure EF 
      var query0 = (from t1 in dc0.Employees
                    where (from t2 in dc0.Orders select t2.EmployeeID).Distinct().Contains(t1.EmployeeID)
                    select t1);
      var result0 = query0.ToList();

      //the same query fails if using EFContextProvider
      dc0 = dc.Context;
      var query = (from t1 in dc0.Employees
                   where (from t2 in dc0.Orders select t2.EmployeeID).Distinct().Contains(t1.EmployeeID)
                   select t1);
      var result = query.ToList();
      return result;
    }

    [HttpGet]
    public IActionResult CustomerFirstOrDefault() {
      var customer = PersistenceManager.Context.Customers.Where(c => c.CompanyName.StartsWith("blah")).FirstOrDefault();
      // return customer;
      return Ok(customer);
    }

    [HttpGet]
    public Int32 OrdersCountForCustomer(string companyName) {
      var customer =
        PersistenceManager.Context.Customers.Include("Orders").Where(c => c.CompanyName.StartsWith(companyName)).First();
      return customer.Orders.Count;
    }

    [HttpGet]
    // AltCustomers will not be in the resourceName/entityType map;
    public IQueryable<Customer> AltCustomers() {
      return PersistenceManager.Context.Customers;
    }


    [HttpGet]
    // public IQueryable<Employee> SearchEmployees([FromUri] int[] employeeIds) {
    // // may need to use FromRoute... as opposed to FromQuery
    public IQueryable<Employee> SearchEmployees([FromQuery] int[] employeeIds) {
      var query = PersistenceManager.Context.Employees.AsQueryable();
      if (employeeIds.Length > 0) {
        query = query.Where(emp => employeeIds.Contains(emp.EmployeeID));
        var result = query.ToList();
      }
      return query;
    }

    [HttpGet]
    public IQueryable<Customer> SearchCustomers([FromQuery] CustomerQBE qbe) {
      // var query = ContextProvider.Context.Customers.Where(c =>
      //    c.CompanyName.StartsWith(qbe.CompanyName));
      var ok = qbe != null && qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1;
      if (!ok) {
        throw new Exception("qbe error");
      }
      // just testing that qbe actually made it in not attempted to write qbe logic here
      // so just return first 3 customers.
      return PersistenceManager.Context.Customers.Take(3);
    }

    [HttpGet]
    public IQueryable<Customer> SearchCustomers2([FromQuery] CustomerQBE[] qbeList) {

      if (qbeList.Length < 2) {
        throw new Exception("all least two items must be passed in");
      }
      var ok = qbeList.All(qbe => {
        return qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1;
      });
      if (!ok) {
        throw new Exception("qbeList error");
      }
      // just testing that qbe actually made it in not attempted to write qbe logic here
      // so just return first 3 customers.
      return PersistenceManager.Context.Customers.Take(3);
    }

    public class CustomerQBE {
      public String CompanyName { get; set; }
      public String[] ContactNames { get; set; }
      public String City { get; set; }
    }

    [HttpGet]
    public IQueryable<Customer> CustomersOrderedStartingWith(string companyName) {
      var customers = PersistenceManager.Context.Customers.Where(c => c.CompanyName.StartsWith(companyName)).OrderBy(cust => cust.CompanyName);
      var list = customers.ToList();
      return customers;
    }

    [HttpGet]
    public IQueryable<Employee> EmployeesMultipleParams(int employeeID, string city) {
      // HACK: 
      if (city == "null") {
        city = null;
      }
      var emps = PersistenceManager.Context.Employees.Where(emp => emp.EmployeeID == employeeID || emp.City.Equals(city));
      return emps;
    }

    [HttpGet]
    public IEnumerable<Object> Lookup1Array() {
      var regions = PersistenceManager.Context.Regions;

      var lookups = new List<Object>();
      lookups.Add(new { regions = regions });
      return lookups;
    }

    [HttpGet]
    public object Lookups() {
      var regions = PersistenceManager.Context.Regions;
      var territories = PersistenceManager.Context.Territories;
      var categories = PersistenceManager.Context.Categories;

      var lookups = new { regions, territories, categories };
      return lookups;
    }

    [HttpGet]
    public IEnumerable<Object> LookupsEnumerableAnon() {
      var regions = PersistenceManager.Context.Regions;
      var territories = PersistenceManager.Context.Territories;
      var categories = PersistenceManager.Context.Categories;

      var lookups = new List<Object>();
      lookups.Add(new { regions = regions, territories = territories, categories = categories });
      return lookups;
    }

    [HttpGet]
    public IQueryable<Object> CompanyNames() {
      var stuff = PersistenceManager.Context.Customers.Select(c => c.CompanyName);
      return stuff;
    }

    [HttpGet]
    public IQueryable<Object> CompanyNamesAndIds() {
      var stuff = PersistenceManager.Context.Customers.Select(c => new { c.CompanyName, c.CustomerID });
      return stuff;
    }

    [HttpGet]
    public IQueryable<CustomerDTO> CompanyNamesAndIdsAsDTO() {
      var stuff = PersistenceManager.Context.Customers.Select(c => new CustomerDTO() { CompanyName = c.CompanyName, CustomerID = c.CustomerID });
      return stuff;
    }

    public class CustomerDTO {
      public CustomerDTO() {
      }

      public CustomerDTO(String companyName, Guid customerID) {
        CompanyName = companyName;
        CustomerID = customerID;
      }

      public Guid CustomerID { get; set; }
      public String CompanyName { get; set; }
      public AnotherType AnotherItem { get; set; }
    }

    public class AnotherType {

    }


    [HttpGet]
    public IQueryable<Object> CustomersWithBigOrders() {
      var stuff = PersistenceManager.Context.Customers.Where(c => c.Orders.Any(o => o.Freight > 100)).Select(c => new { Customer = c, BigOrders = c.Orders.Where(o => o.Freight > 100) });
      return stuff;
    }



    [HttpGet]
#if NHIBERNATE
    public IQueryable<Object> CompanyInfoAndOrders(System.Web.Http.OData.Query.ODataQueryOptions options) {
        // Need to handle this specially for NH, to prevent $top being applied to Orders
        var query = ContextProvider.Context.Customers;
        var queryHelper = new NHQueryHelper();

        // apply the $filter, $skip, $top to the query
        var query2 = queryHelper.ApplyQuery(query, options);

        // execute query, then expand the Orders
        var r = query2.Cast<Customer>().ToList();
        NHInitializer.InitializeList(r, "Orders");

        // after all is loaded, create the projection
        var stuff = r.AsQueryable().Select(c => new { c.CompanyName, c.CustomerID, c.Orders });
        queryHelper.ConfigureFormatter(Request, query);
#else
    public IQueryable<Object> CompanyInfoAndOrders() {
      var stuff = PersistenceManager.Context.Customers.Select(c => new { c.CompanyName, c.CustomerID, c.Orders });
#endif
      return stuff;
    }

    [HttpGet]
    public Object CustomersAndProducts() {
      var stuff = new { Customers = PersistenceManager.Context.Customers.ToList(), Products = PersistenceManager.Context.Products.ToList() };
      return stuff;
    }

    [HttpGet]
    public IQueryable<Object> TypeEnvelopes() {
      var stuff = this.GetType().Assembly.GetTypes()
        .Select(t => new { t.Assembly.FullName, t.Name, t.Namespace })
        .AsQueryable();
      return stuff;
    }


    [HttpGet]
    public IQueryable<Customer> CustomersAndOrders() {
      var custs = PersistenceManager.Context.Customers.Include("Orders");
      return custs;
    }

    [HttpGet]
    public IQueryable<Order> OrdersAndCustomers() {
      var orders = PersistenceManager.Context.Orders.Include("Customer");
      return orders;
    }


    [HttpGet]
    public IQueryable<Customer> CustomersStartingWithA() {
      var custs = PersistenceManager.Context.Customers.Where(c => c.CompanyName.StartsWith("A"));
      return custs;
    }

    [HttpGet]
    // [EnableBreezeQuery]
    // public HttpResponseMessage CustomersAsHRM() {
    public IActionResult CustomersAsHRM() {
      var customers = PersistenceManager.Context.Customers.Cast<Customer>();
      return Ok(customers);
    }

    [HttpGet]
    public IQueryable<OrderDetail> OrderDetailsMultiple(int multiple, string expands) {
      var query = PersistenceManager.Context.OrderDetails.OfType<OrderDetail>();
      if (!string.IsNullOrWhiteSpace(expands)) {
        var segs = expands.Split(',').ToList();
        segs.ForEach(s => {
          query = ((System.Data.Entity.Infrastructure.DbQuery<OrderDetail>)query).Include(s);
        });
      }
      var orig = query.ToList();
      var list = new List<OrderDetail>(orig.Count * multiple);
      for (var i = 0; i < multiple; i++) {
        for (var j = 0; j < orig.Count; j++) {
          var od = orig[j];
          var newProductID = i * j + 1;
          var clone = new OrderDetail();
          clone.Order = od.Order;
          clone.OrderID = od.OrderID;
          clone.RowVersion = od.RowVersion;
          clone.UnitPrice = od.UnitPrice;
          clone.Quantity = (short)multiple;
          clone.Discount = i;
          clone.ProductID = newProductID;

          if (od.Product != null) {
            var p = new Product();
            var op = od.Product;
            p.ProductID = newProductID;
            p.Category = op.Category;
            p.CategoryID = op.CategoryID;
            p.Discontinued = op.Discontinued;
            p.DiscontinuedDate = op.DiscontinuedDate;
            p.ProductName = op.ProductName;
            p.QuantityPerUnit = op.QuantityPerUnit;
            p.ReorderLevel = op.ReorderLevel;
            p.RowVersion = op.RowVersion;
            p.Supplier = op.Supplier;
            p.SupplierID = op.SupplierID;
            p.UnitPrice = op.UnitPrice;
            p.UnitsInStock = op.UnitsInStock;
            p.UnitsOnOrder = op.UnitsOnOrder;
            clone.Product = p;
          }

          list.Add(clone);
        }
      }
      return list.AsQueryable();
    }

    #endregion
  }

#if CODEFIRST_PROVIDER
  public class NorthwindPersistenceManager : EFPersistenceManager<NorthwindIBContext_CF> {
    public const string CONFIG_VERSION = "CODEFIRST_PROVIDER";
    public NorthwindPersistenceManager(NorthwindIBContext_CF dbContext) : base(dbContext) { }
#elif DATABASEFIRST_NEW
  public class NorthwindPersistenceManager : EFPersistenceManager<NorthwindIBContext_EDMX_2012> {
    public const string CONFIG_VERSION = "DATABASEFIRST_NEW";
    public NorthwindContextProvider() : base() { }
#endif

    protected override void AfterSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap, List<KeyMapping> keyMappings) {
      var tag = (string)SaveOptions.Tag;
      if (tag == "CommentKeyMappings.After") {

        foreach (var km in keyMappings) {
          var realint = Convert.ToInt32(km.RealValue);
          byte seq = (byte)(realint % 512);
          AddComment(km.EntityTypeName + ':' + km.RealValue, seq);
        }
      } else if (tag == "UpdateProduceKeyMapping.After") {
        if (!keyMappings.Any()) throw new Exception("UpdateProduce.After: No key mappings available");
        var km = keyMappings[0];
        UpdateProduceDescription(km.EntityTypeName + ':' + km.RealValue);

      } else if (tag == "LookupEmployeeInSeparateContext.After") {
        LookupEmployeeInSeparateContext(false);
      } else if (tag == "LookupEmployeeInSeparateContext.SameConnection.After") {
        LookupEmployeeInSeparateContext(true);
      } else if (tag == "deleteProductOnServer") {
        var t = typeof(Product);
        var prodinfo = saveMap[t].First();
        prodinfo.EntityState = EntityState.Deleted;
      } else if (tag != null && tag.StartsWith("deleteProductOnServer:")) {
        // create new EntityInfo for entity that we want to delete that was not in the save bundle
        var id = tag.Substring(tag.IndexOf(':') + 1);
        var product = new Product();
        product.ProductID = int.Parse(id);
        var infos = GetEntityInfos(saveMap, typeof(Product));
        var info = CreateEntityInfo(product, EntityState.Deleted);
        infos.Add(info);
      } else if (tag == "deleteSupplierAndProductOnServer") {
        // mark deleted entities that are in the save bundle
        var t = typeof(Product);
        var infos = GetEntityInfos(saveMap, typeof(Product));
        var prodinfo = infos.FirstOrDefault();
        if (prodinfo != null) prodinfo.EntityState = EntityState.Deleted;
        infos = GetEntityInfos(saveMap, typeof(Supplier));
        var supinfo = infos.FirstOrDefault();
        if (supinfo != null) supinfo.EntityState = EntityState.Deleted;
      }
      base.AfterSaveEntities(saveMap, keyMappings);
    }

    public List<EntityInfo> GetEntityInfos(Dictionary<Type, List<EntityInfo>> saveMap, Type t) {
      List<EntityInfo> entityInfos;
      if (!saveMap.TryGetValue(t, out entityInfos)) {
        entityInfos = new List<EntityInfo>();
        saveMap.Add(t, entityInfos);
      }
      return entityInfos;
    }

    public Dictionary<Type, List<EntityInfo>> GetSaveMapFromSaveBundle(JObject saveBundle) {
      InitializeSaveState(saveBundle); // Sets initial EntityInfos
      SaveWorkState.BeforeSave();      // Creates the SaveMap as byproduct of BeforeSave logic
      return SaveWorkState.SaveMap;
    }

#if (CODEFIRST_PROVIDER || DATABASEFIRST_NEW || DATABASEFIRST_OLD)
    /* hack to set the current DbTransaction onto the DbCommand.  Transaction comes from EF private properties. */
    public void SetCurrentTransaction(System.Data.Common.DbCommand command) {
      if (EntityTransaction != null) {
        // get private member via reflection
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var etype = EntityTransaction.GetType();
        var stProp = etype.GetProperty("StoreTransaction", bindingFlags);
        var transaction = stProp.GetValue(EntityTransaction, null);
        var dbTransaction = transaction as System.Data.Common.DbTransaction;
        if (dbTransaction != null) {
          command.Transaction = dbTransaction;
        }
      }
    }
#endif

    // Test performing a raw db insert to NorthwindIB using the base connection
    private int AddComment(string comment, byte seqnum) {
#if ORACLE_EDMX
      var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      var text = String.Format("insert into COMMENT_ (CreatedOn, Comment1, SeqNum) values (TO_DATE('{0}','YYYY-MM-DD HH24:MI:SS'), '{1}', {2})",
          time, comment, seqnum);
#else
      var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
      var text = String.Format("insert into Comment (CreatedOn, Comment1, SeqNum) values ('{0}', '{1}', {2})",
          time, comment, seqnum);
#endif
#if NHIBERNATE
      var cmd = Session.CreateSQLQuery(text);
      var result = cmd.ExecuteUpdate();
#else
      var conn = StoreConnection;
      var cmd = conn.CreateCommand();
#if !ORACLE_EDMX
      SetCurrentTransaction(cmd);
#endif
      cmd.CommandText = text;
      var result = cmd.ExecuteNonQuery();
#endif
      return result;
    }

    // Test performing a raw db update to ProduceTPH using the ProduceTPH connection.  Requires DTC.
    private int UpdateProduceDescription(string comment) {
      using (var conn = new SqlConnection("data source=.;initial catalog=ProduceTPH;integrated security=True;multipleactiveresultsets=True;application name=EntityFramework")) {
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = String.Format("update ItemOfProduce set Description='{0}' where id='{1}'",
            comment, "13F1C9F5-3189-45FA-BA6E-13314FAFAA92");
        var result = cmd.ExecuteNonQuery();
        conn.Close();
        return result;
      }
    }

    // Use another Context to simulate lookup.  Returns Margaret Peacock if employeeId is not specified.
    private Employee LookupEmployeeInSeparateContext(bool existingConnection, int employeeId = 4) {
      var context2 = existingConnection
#if CODEFIRST_PROVIDER
 ? new NorthwindIBContext_CF(EntityConnection)
        : new NorthwindIBContext_CF();
#elif DATABASEFIRST_OLD
        ? new NorthwindIBContext_EDMX((System.Data.EntityClient.EntityConnection)EntityConnection)
        : new NorthwindIBContext_EDMX();
#elif DATABASEFIRST_NEW
        ? new NorthwindIBContext_EDMX_2012(EntityConnection)
        : new NorthwindIBContext_EDMX_2012();
#elif ORACLE_EDMX
        ? new NorthwindIBContext_EDMX_Oracle(EntityConnection)
        : new NorthwindIBContext_EDMX_Oracle();
#elif NHIBERNATE
        ? new NorthwindNHContext(this)
        : new NorthwindNHContext();
#endif

      var query = context2.Employees.Where(e => e.EmployeeID == employeeId);
      var employee = query.FirstOrDefault();
      return employee;
    }

    protected override bool BeforeSaveEntity(EntityInfo entityInfo) {
      if ((string)SaveOptions.Tag == "addProdOnServer") {
        Supplier supplier = entityInfo.Entity as Supplier;
        Product product = new Product() {
          ProductName = "Product added on server"
        };
#if CODEFIRST_PROVIDER
        if (supplier.Products == null) supplier.Products = new List<Product>();
#endif
        supplier.Products.Add(product);
        return true;
      }

      // prohibit any additions of entities of type 'Region'
      if (entityInfo.Entity.GetType() == typeof(Region) && entityInfo.EntityState == EntityState.Added) {
        var region = entityInfo.Entity as Region;
        if (region.RegionDescription.ToLowerInvariant().StartsWith("error")) return false;
      }

#if ORACLE_EDMX
      // Convert GUIDs in Customer and Order to be compatible with Oracle
      if (entityInfo.Entity.GetType() == typeof(Customer)) {
        var cust = entityInfo.Entity as Customer;
        if (cust.CustomerID != null) {
          cust.CustomerID = cust.CustomerID.ToUpperInvariant();
        }
      } else if (entityInfo.Entity.GetType() == typeof(Order)) {
        var order = entityInfo.Entity as Order;
        if (order.CustomerID != null) {
          order.CustomerID = order.CustomerID.ToUpperInvariant();
        }
      }
#endif

      return base.BeforeSaveEntity(entityInfo);
    }

    protected override Dictionary<Type, List<EntityInfo>> BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap) {

      var tag = (string)SaveOptions.Tag;

      if (tag == "CommentOrderShipAddress.Before") {
        var orderInfos = saveMap[typeof(Order)];
        byte seq = 1;
        foreach (var info in orderInfos) {
          var order = (Order)info.Entity;
          AddComment(order.ShipAddress, seq++);
        }
      } else if (tag == "UpdateProduceShipAddress.Before") {
        var orderInfos = saveMap[typeof(Order)];
        var order = (Order)orderInfos[0].Entity;
        UpdateProduceDescription(order.ShipAddress);
      } else if (tag == "LookupEmployeeInSeparateContext.Before") {
        LookupEmployeeInSeparateContext(false);
      } else if (tag == "LookupEmployeeInSeparateContext.SameConnection.Before") {
        LookupEmployeeInSeparateContext(true);
      } else if (tag == "ValidationError.Before") {
        foreach (var type in saveMap.Keys) {
          var list = saveMap[type];
          foreach (var entityInfo in list) {
            var entity = entityInfo.Entity;
            var entityError = new EntityError() {
              EntityTypeName = type.Name,
              ErrorMessage = "Error message for " + type.Name,
              ErrorName = "Server-Side Validation",
            };
            if (entity is Order) {
              var order = (Order)entity;
              entityError.KeyValues = new object[] { order.OrderID };
              entityError.PropertyName = "OrderDate";
            }

          }
        }
      } else if (tag == "increaseProductPrice") {
        Dictionary<Type, List<EntityInfo>> saveMapAdditions = new Dictionary<Type, List<EntityInfo>>();
        foreach (var type in saveMap.Keys) {
          if (type == typeof(Category)) {
            foreach (var entityInfo in saveMap[type]) {
              if (entityInfo.EntityState == EntityState.Modified) {
                Category category = (entityInfo.Entity as Category);
                var products = this.Context.Products.Where(p => p.CategoryID == category.CategoryID);
                foreach (var product in products) {
                  if (!saveMapAdditions.ContainsKey(typeof(Product)))
                    saveMapAdditions[typeof(Product)] = new List<EntityInfo>();

                  var ei = this.CreateEntityInfo(product, EntityState.Modified);
                  ei.ForceUpdate = true;
                  var incr = (Convert.ToInt64(product.UnitPrice) % 2) == 0 ? 1 : -1;
                  product.UnitPrice += incr;
                  saveMapAdditions[typeof(Product)].Add(ei);
                }
              }
            }
          }
        }
        foreach (var type in saveMapAdditions.Keys) {
          if (!saveMap.ContainsKey(type)) {
            saveMap[type] = new List<EntityInfo>();
          }
          foreach (var enInfo in saveMapAdditions[type]) {
            saveMap[type].Add(enInfo);
          }
        }
      } else if (tag == "deleteProductOnServer.Before") {
        var prodinfo = saveMap[typeof(Product)].First();
        if (prodinfo.EntityState == EntityState.Added) {
          // because Deleted throws error when trying delete non-existent row from database
          prodinfo.EntityState = EntityState.Detached;
        } else {
          prodinfo.EntityState = EntityState.Deleted;
        }
      } else if (tag == "deleteSupplierOnServer.Before") {
        var product = (Product)saveMap[typeof(Product)].First().Entity;
        var infos = GetEntityInfos(saveMap, typeof(Supplier));
        var supinfo = infos.FirstOrDefault();
        if (supinfo != null) {
          supinfo.EntityState = EntityState.Deleted;
        } else {
          // create new EntityInfo for entity that we want to delete that was not in the save bundle
          var supplier = new Supplier();
          supplier.Location = new Location();
          supplier.SupplierID = product.SupplierID.GetValueOrDefault();
          supinfo = CreateEntityInfo(supplier, EntityState.Deleted);
          infos.Add(supinfo);
        }
      }
#if DATABASEFIRST_OLD
      DataAnnotationsValidator.AddDescriptor(typeof(Customer), typeof(CustomerMetaData));
      var validator = new DataAnnotationsValidator(this);
      validator.ValidateEntities(saveMap, true);
#endif
      return base.BeforeSaveEntities(saveMap);
    }

  }

}

