// Only one of the next few should be uncommented.
#define CODEFIRST_PROVIDER
//#define DATABASEFIRST_NEW
//#define ORACLE_EDMX
//#define NHIBERNATE

using System;
using System.Net;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Http;

using Breeze.ContextProvider;
using Breeze.WebApi2;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Data.SqlClient;
using System.IO;
using System.Web;

using NHibernate.Mapping;
using NHibernate.Transform;
#if CODEFIRST_PROVIDER
using Breeze.ContextProvider.EF6;
using Models.NorthwindIB.CF;
using Foo;
using System.ComponentModel.DataAnnotations;
using System.Web.Http.OData.Query;
#elif DATABASEFIRST_OLD
using Breeze.ContextProvider.EF6;
using Models.NorthwindIB.EDMX;
#elif DATABASEFIRST_NEW
using Breeze.ContextProvider.EF6;
using Models.NorthwindIB.EDMX_2012;
#elif ORACLE_EDMX
using Breeze.ContextProvider.EF6;
using Models.NorthwindIB.Oracle;
#elif NHIBERNATE
using Breeze.ContextProvider.NH;
using NHibernate;
using NHibernate.Linq;
using Models.NorthwindIB.NH;
#endif

namespace Sample_WebApi2.Controllers {

#if NHIBERNATE
  [BreezeNHController]
#else
  [BreezeController]
#endif

  public class NorthwindIBModelController : ApiController {
    private NorthwindContextProvider ContextProvider;

    public NorthwindIBModelController() {
      ContextProvider = new NorthwindContextProvider();
    }

    //[HttpGet]
    //public String Metadata() {
    //  var folder = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data");
    //  var fileName = Path.Combine(folder, "metadata.json");
    //  var jsonMetadata = File.ReadAllText(fileName);
    //  return jsonMetadata;
    //}

    //[HttpGet]
    //public HttpResponseMessage Metadata() {
    //  var result = new HttpResponseMessage { Content = new StringContent(ContextProvider.Metadata())};
    //  result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    //  return result;
    //}

    [HttpGet]
    public String Metadata() {
      return ContextProvider.Metadata();
    }
    [HttpPost]
    public SaveResult SaveChanges(JObject saveBundle) {
      return ContextProvider.SaveChanges(saveBundle);
    }

    #region Save interceptors 
    [HttpPost]
    public SaveResult SaveWithTransactionScope(JObject saveBundle) {
      var txSettings = new TransactionSettings() { TransactionType = TransactionType.TransactionScope };
      return ContextProvider.SaveChanges(saveBundle, txSettings);
    }

    [HttpPost]
    public SaveResult SaveWithDbTransaction(JObject saveBundle) {
      var txSettings = new TransactionSettings() { TransactionType = TransactionType.DbTransaction };
      return ContextProvider.SaveChanges(saveBundle, txSettings);
    }

    [HttpPost]
    public SaveResult SaveWithNoTransaction(JObject saveBundle) {
      var txSettings = new TransactionSettings() { TransactionType = TransactionType.None };
      return ContextProvider.SaveChanges(saveBundle, txSettings);
    }

    [HttpPost]
    public SaveResult SaveWithComment(JObject saveBundle) {
      ContextProvider.BeforeSaveEntitiesDelegate = AddComment;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveWithExit(JObject saveBundle) {
      // set break points here to see how these two approaches give you a SaveMap w/o saving.
      var saveMap =  ContextProvider.GetSaveMapFromSaveBundle(saveBundle);
      saveMap = new NorthwindIBDoNotSaveContext().GetSaveMapFromSaveBundle(saveBundle);
      return new SaveResult() { Entities = new List<Object>(), KeyMappings = new List<KeyMapping>() };
    }

    [HttpPost]
    public SaveResult SaveAndThrow(JObject saveBundle) {
      ContextProvider.BeforeSaveEntitiesDelegate = ThrowError;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveWithEntityErrorsException(JObject saveBundle) {
      ContextProvider.BeforeSaveEntitiesDelegate = ThrowEntityErrorsException;
      return ContextProvider.SaveChanges(saveBundle);
    }


    [HttpPost]
    public SaveResult SaveWithFreight(JObject saveBundle) {
      ContextProvider.BeforeSaveEntityDelegate = CheckFreight;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveWithFreight2(JObject saveBundle) {
      ContextProvider.BeforeSaveEntitiesDelegate = CheckFreightOnOrders;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckInitializer(JObject saveBundle) {
      ContextProvider.BeforeSaveEntitiesDelegate = AddOrder;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckUnmappedProperty(JObject saveBundle) {
      ContextProvider.BeforeSaveEntityDelegate = CheckUnmappedProperty;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckUnmappedPropertySerialized(JObject saveBundle) {
      ContextProvider.BeforeSaveEntityDelegate = CheckUnmappedPropertySerialized;
      return ContextProvider.SaveChanges(saveBundle);
    }

    [HttpPost]
    public SaveResult SaveCheckUnmappedPropertySuppressed(JObject saveBundle) {
      ContextProvider.BeforeSaveEntityDelegate = CheckUnmappedPropertySuppressed;
      return ContextProvider.SaveChanges(saveBundle);
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
        var ex =  new EntityErrorsException("test of custom exception message", errors);
        // if you want to see a different error status code use this.
        // ex.StatusCode = HttpStatusCode.Conflict; // Conflict = 409 ; default is Forbidden (403).
        throw ex;
      }
      return saveMap;
    }


    private Dictionary<Type, List<EntityInfo>> AddOrder(Dictionary<Type, List<EntityInfo>> saveMap) {
      var order = new Order();
      order.OrderDate = DateTime.Today;
      var ei = ContextProvider.CreateEntityInfo(order);
      List<EntityInfo> orderInfos;
      if (!saveMap.TryGetValue(typeof(Order), out orderInfos)) {
        orderInfos = new List<EntityInfo>();
        saveMap.Add(typeof(Order), orderInfos);
      }
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
      if ((ContextProvider.SaveOptions.Tag as String) == "freight update") {
        var order = entityInfo.Entity as Order;
        order.Freight = order.Freight + 1;
      } else if ((ContextProvider.SaveOptions.Tag as String) == "freight update-ov") {
        var order = entityInfo.Entity as Order;
        order.Freight = order.Freight + 1;
        entityInfo.OriginalValuesMap["Freight"] = null;
      } else if ((ContextProvider.SaveOptions.Tag as String) == "freight update-force") {
        var order = entityInfo.Entity as Order;
        order.Freight = order.Freight + 1;
        entityInfo.ForceUpdate = true;
      }
      return true;
    }

    private Dictionary<Type, List<EntityInfo>> AddComment(Dictionary<Type, List<EntityInfo>> saveMap) {
      var comment = new Comment();
      var tag = ContextProvider.SaveOptions.Tag;
      comment.Comment1 = (tag == null) ? "Generic comment" : tag.ToString();
      comment.CreatedOn = DateTime.Now;
      comment.SeqNum = 1;
      var ei = ContextProvider.CreateEntityInfo(comment);
      List<EntityInfo> commentInfos;
      if (!saveMap.TryGetValue(typeof(Comment), out commentInfos)) {
        commentInfos = new List<EntityInfo>();
        saveMap.Add(typeof(Comment), commentInfos);
      }
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

      if (((dynamic) anotherOne).z[5].foo.Value != 4) {
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
#if NHIBERNATE
    [BreezeNHQueryable(MaxAnyAllExpressionDepth = 3)]
#else
    [EnableBreezeQuery(MaxAnyAllExpressionDepth = 3)]
#endif
    public IQueryable<Customer> Customers() {
      return ContextProvider.Context.Customers;
    }

    [HttpGet]
#if NHIBERNATE
    [BreezeNHQueryable(MaxExpansionDepth = 3)]
#else
    [EnableBreezeQuery(MaxExpansionDepth = 3)]
#endif
    public IQueryable<Order> Orders() {
      return ContextProvider.Context.Orders;
    }

    [HttpGet]
    public IQueryable<Employee> Employees() {
      return ContextProvider.Context.Employees;
    }

    [HttpGet]
    public IQueryable<OrderDetail> OrderDetails() {
      return ContextProvider.Context.OrderDetails;
    }

    [HttpGet]
    public IQueryable<Product> Products() {
      return ContextProvider.Context.Products;
    }

    [HttpGet]
    public IQueryable<Supplier> Suppliers() {
      return ContextProvider.Context.Suppliers;
    }

    [HttpGet]
    public IQueryable<Region> Regions() {
      return ContextProvider.Context.Regions;
    }

    [HttpGet]
    public IQueryable<Territory> Territories() {
      return ContextProvider.Context.Territories;
    }

    [HttpGet]
    public IQueryable<Category> Categories() {
      return ContextProvider.Context.Categories;
    }

    [HttpGet]
    public IQueryable<Role> Roles() {
      return ContextProvider.Context.Roles;
    }

    [HttpGet]
    public IQueryable<User> Users() {
      return ContextProvider.Context.Users;
    }

    [HttpGet]
    public IQueryable<TimeLimit> TimeLimits() {
      return ContextProvider.Context.TimeLimits;
    }

    [HttpGet]
    public IQueryable<TimeGroup> TimeGroups() {
      return ContextProvider.Context.TimeGroups;
    }

    [HttpGet]
    public IQueryable<Comment> Comments() {
      return ContextProvider.Context.Comments;
    }

    [HttpGet]
    public IQueryable<UnusualDate> UnusualDates() {
      return ContextProvider.Context.UnusualDates;
    }

#if ! DATABASEFIRST_OLD
    [HttpGet]
    public IQueryable<Geospatial> Geospatials() {
      return ContextProvider.Context.Geospatials;
    }
#endif

    #endregion

    #region named queries

    [HttpGet]
    public IQueryable<Customer> CustomersStartingWith(string companyName) {
      if (companyName == "null") {
        throw new Exception("nulls should not be passed as 'null'");
      }
      if (String.IsNullOrEmpty(companyName)) {
        companyName = "";
      }
      var custs = ContextProvider.Context.Customers.Where(c => c.CompanyName.StartsWith(companyName));
      return custs;
    }

    [HttpGet]
    public Object CustomerCountsByCountry() {
      return ContextProvider.Context.Customers.GroupBy(c => c.Country).Select(g => new { g.Key, Count = g.Count() });
    }


    [HttpGet]
    public Customer CustomerWithScalarResult() {
      return ContextProvider.Context.Customers.First();
    }

    [HttpGet]
    public IQueryable<Customer> CustomersWithHttpError() {
      var responseMsg = new HttpResponseMessage(HttpStatusCode.NotFound);
      responseMsg.Content = new StringContent("Custom error message");
      responseMsg.ReasonPhrase = "Custom Reason";
      throw new HttpResponseException(responseMsg);
    }

    [HttpGet]
    [EnableBreezeQuery]
    public IEnumerable<Employee> EnumerableEmployees() {
      return ContextProvider.Context.Employees.ToList();
    }

    [HttpGet]
    public IQueryable<Employee> EmployeesFilteredByCountryAndBirthdate(DateTime birthDate, string country) {
      return ContextProvider.Context.Employees.Where(emp => emp.BirthDate >= birthDate && emp.Country == country);
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
      var dc = new EFContextProvider<NorthwindIBContext_CF>();
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
    public Customer CustomerFirstOrDefault() {
      var customer = ContextProvider.Context.Customers.Where(c => c.CompanyName.StartsWith("blah")).FirstOrDefault();
      return customer;
    }

    [HttpGet]
    public Int32 OrdersCountForCustomer(string companyName) {
      var customer =
        ContextProvider.Context.Customers.Include("Orders").Where(c => c.CompanyName.StartsWith(companyName)).First();
      return customer.Orders.Count;
    }

    [HttpGet]
    // AltCustomers will not be in the resourceName/entityType map;
    public IQueryable<Customer> AltCustomers() {
      return ContextProvider.Context.Customers;
    }


    [HttpGet]
    public IQueryable<Employee> SearchEmployees([FromUri] int[] employeeIds) {
      var query = ContextProvider.Context.Employees.AsQueryable();
      if (employeeIds.Length > 0) {
        query = query.Where(emp => employeeIds.Contains(emp.EmployeeID));
        var result = query.ToList();
      }
      return query;
    }

    [HttpGet]
    public IQueryable<Customer> SearchCustomers([FromUri] CustomerQBE qbe) {
      // var query = ContextProvider.Context.Customers.Where(c =>
      //    c.CompanyName.StartsWith(qbe.CompanyName));
      var ok = qbe != null && qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1;
      if (!ok) {
        throw new Exception("qbe error");
      }
      // just testing that qbe actually made it in not attempted to write qbe logic here
      // so just return first 3 customers.
      return ContextProvider.Context.Customers.Take(3);
    }

    [HttpGet]
    public IQueryable<Customer> SearchCustomers2([FromUri] CustomerQBE[] qbeList) {

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
      return ContextProvider.Context.Customers.Take(3);
    }

    public class CustomerQBE {
      public String CompanyName { get; set; }
      public String[] ContactNames { get; set; }
      public String City { get; set; }
    }

    [HttpGet]
    public IQueryable<Customer> CustomersOrderedStartingWith(string companyName) {
      var customers = ContextProvider.Context.Customers.Where(c => c.CompanyName.StartsWith(companyName)).OrderBy(cust => cust.CompanyName);
      var list = customers.ToList();
      return customers;
    }

    [HttpGet]
    public IQueryable<Employee> EmployeesMultipleParams(int employeeID, string city) {
      // HACK: 
      if (city == "null") {
        city = null;
      }
      var emps = ContextProvider.Context.Employees.Where(emp => emp.EmployeeID == employeeID || emp.City.Equals(city));
      return emps;
    }

    [HttpGet]
    public IEnumerable<Object> Lookup1Array() {
      var regions = ContextProvider.Context.Regions;

      var lookups = new List<Object>();
      lookups.Add(new {regions = regions});
      return lookups;
    }

    [HttpGet]
    public object Lookups() {
      var regions = ContextProvider.Context.Regions;
      var territories = ContextProvider.Context.Territories;
      var categories = ContextProvider.Context.Categories;

      var lookups = new { regions, territories, categories };
      return lookups;
    }

    [HttpGet]
    public IEnumerable<Object> LookupsEnumerableAnon() {
      var regions = ContextProvider.Context.Regions;
      var territories = ContextProvider.Context.Territories;
      var categories = ContextProvider.Context.Categories;

      var lookups = new List<Object>();
      lookups.Add(new {regions = regions, territories = territories, categories = categories});
      return lookups;
    }

    [HttpGet]
    public IQueryable<Object> CompanyNames() {
      var stuff = ContextProvider.Context.Customers.Select(c => c.CompanyName);
      return stuff;
    }

    [HttpGet]
    public IQueryable<Object> CompanyNamesAndIds() {
      var stuff = ContextProvider.Context.Customers.Select(c => new { c.CompanyName, c.CustomerID });
      return stuff;
    }

    [HttpGet]
    public IQueryable<CustomerDTO> CompanyNamesAndIdsAsDTO() {
      var stuff = ContextProvider.Context.Customers.Select(c => new CustomerDTO() { CompanyName = c.CompanyName, CustomerID = c.CustomerID });
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
      var stuff = ContextProvider.Context.Customers.Where(c => c.Orders.Any(o => o.Freight > 100)).Select(c => new { Customer = c, BigOrders = c.Orders.Where(o => o.Freight > 100) });
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
      var stuff = ContextProvider.Context.Customers.Select(c => new { c.CompanyName, c.CustomerID, c.Orders });
#endif
      return stuff;
    }

    [HttpGet]
    public Object CustomersAndProducts() {
      var stuff = new { Customers = ContextProvider.Context.Customers.ToList(), Products = ContextProvider.Context.Products.ToList() };
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
      var custs = ContextProvider.Context.Customers.Include("Orders");
      return custs;
    }

    [HttpGet]
    public IQueryable<Order> OrdersAndCustomers() {
      var orders = ContextProvider.Context.Orders.Include("Customer");
      return orders;
    }


    [HttpGet]
    public IQueryable<Customer> CustomersStartingWithA() {
      var custs = ContextProvider.Context.Customers.Where(c => c.CompanyName.StartsWith("A"));
      return custs;
    }

    [HttpGet]
#if NHIBERNATE
    [BreezeNHQueryable]
#else
    [EnableBreezeQuery]
#endif
    public HttpResponseMessage CustomersAsHRM() {
      var customers = ContextProvider.Context.Customers.Cast<Customer>();
      var response = Request.CreateResponse(HttpStatusCode.OK, customers);
      return response;
    }

    [HttpGet]
    public IQueryable<OrderDetail> OrderDetailsMultiple(int multiple, string expands)
    {
        var query = ContextProvider.Context.OrderDetails.OfType<OrderDetail>();
        if (!string.IsNullOrWhiteSpace(expands)) {
            var segs = expands.Split(',').ToList();
            segs.ForEach(s => {
                query = ((System.Data.Entity.Infrastructure.DbQuery<OrderDetail>) query).Include(s);
            });
        }
        var orig = query.ToList();
        var list = new List<OrderDetail>(orig.Count * multiple);
        for (var i = 0; i < multiple; i++)
        {
            for (var j = 0; j < orig.Count; j++)
            {
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
  public class NorthwindContextProvider : EFContextProvider<NorthwindIBContext_CF> {
    public const string CONFIG_VERSION = "CODEFIRST_PROVIDER";
    public NorthwindContextProvider() : base() { }
#elif DATABASEFIRST_OLD
  public class NorthwindContextProvider: EFContextProvider<NorthwindIBContext_EDMX>  {
    public const string CONFIG_VERSION = "DATABASEFIRST_OLD";
    public NorthwindContextProvider() : base() { }
#elif DATABASEFIRST_NEW
  public class NorthwindContextProvider : EFContextProvider<NorthwindIBContext_EDMX_2012> {
    public const string CONFIG_VERSION = "DATABASEFIRST_NEW";
    public NorthwindContextProvider() : base() { }
#elif ORACLE_EDMX
  public class NorthwindContextProvider : EFContextProvider<NorthwindIBContext_EDMX_Oracle> {
    public const string CONFIG_VERSION = "ORACLE_EDMX";
    public NorthwindContextProvider() : base() { }
#elif NHIBERNATE
  public class NorthwindContextProvider : NorthwindNHContext {
    public const string CONFIG_VERSION = "NHIBERNATE";
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
      }
      base.AfterSaveEntities(saveMap, keyMappings);
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
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
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