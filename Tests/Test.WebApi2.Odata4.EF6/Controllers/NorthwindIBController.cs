// Only one of the next 5 should be uncommented.
#define CODEFIRST_PROVIDER
//#define DATABASEFIRST_NEW
//#define ORACLE_EDMX
//#define NHIBERNATE

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.OData;

using System.Web.Http;

#if CODEFIRST_PROVIDER
using Models.NorthwindIB.CF;
using Foo;
using System.ComponentModel.DataAnnotations;
using System.Web.OData.Query;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;
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


namespace Test.WebApi2.OData4.Controllers {

  public abstract class BaseController2<TEntity, TKey1, TKey2> : BaseController<TEntity> where TEntity : class {
    // PATCH odata/TodoItems(5)
    [AcceptVerbs("PATCH", "MERGE")]
    public async Task<IHttpActionResult> Patch([FromODataUri] TKey1 key1, [FromODataUri] TKey2 key2, Delta<TEntity> patch) {
      if (!ModelState.IsValid) {
        return BadRequest(ModelState);
      }

      TEntity item = await Items.FindAsync(key1, key2);
      if (item == null) {
        return NotFound();
      }

      patch.Patch(item);

      try {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException) {
        //if (!TodoItemExists(key)) {
        //  return NotFound();
        //}
        throw;
      }

      return Updated(item);
    }

    // DELETE odata/TodoItems(5)
    public async Task<IHttpActionResult> Delete([FromODataUri] TKey1 key1, [FromODataUri] TKey2 key2) {
      TEntity item = await Items.FindAsync(key1, key2);
      if (item == null) {
        return NotFound();
      }

      Items.Remove(item);
      await _context.SaveChangesAsync();

      return StatusCode(HttpStatusCode.NoContent);
    }
  }

  public abstract class BaseController1<TEntity, TKey> : BaseController<TEntity> where TEntity : class {
    // PATCH odata/TodoItems(5)
    [AcceptVerbs("PATCH", "MERGE")]
    public async Task<IHttpActionResult> Patch([FromODataUri] TKey key, Delta<TEntity> patch) {
      if (!ModelState.IsValid) {
        return BadRequest(ModelState);
      }

      TEntity item = await Items.FindAsync(key);
      if (item == null) {
        return NotFound();
      }

      patch.Patch(item);

      try {
        await _context.SaveChangesAsync();
      } catch (DbUpdateConcurrencyException) {
        //if (!TodoItemExists(key)) {
        //  return NotFound();
        //}
        throw;
      }

      return Updated(item);
    }

    // DELETE odata/TodoItems(5)
    public async Task<IHttpActionResult> Delete([FromODataUri] TKey key) {
      TEntity item = await Items.FindAsync(key);
      if (item == null) {
        return NotFound();
      }

      Items.Remove(item);
      await _context.SaveChangesAsync();

      return StatusCode(HttpStatusCode.NoContent);
    }
  }

  public abstract class BaseController<TEntity> : ODataController where TEntity : class {
    internal readonly NorthwindIBContext_CF _context = new NorthwindIBContext_CF();
    internal DbSet<TEntity> _items;

    public DbSet<TEntity> Items {
      get {
        if (_items == null) {
          _items = _context.Set<TEntity>();
        }
        return _items;
      }
    }

    [EnableQuery]
    public virtual IQueryable<TEntity> Get() {
      return _context.Set<TEntity>();
    }

    // POST odata/TodoItems
    public async Task<IHttpActionResult> Post(TEntity item) {
      if (!ModelState.IsValid) {
        return BadRequest(ModelState);
      }

      Items.Add(item);
      await _context.SaveChangesAsync();

      return Created(item);
    }



    //private bool ItemExists(int key) {
    //  return Items.Count(e => e.Id == key) > 0;
    //}


  }

  public class ProductsController : BaseController1<Product, Int32> {

  }

  public class CustomersController : BaseController1<Customer, Guid> {

  }

  public class OrdersController : BaseController1<Order, Int32> {

    [EnableQuery(MaxExpansionDepth = 5)]
    public override IQueryable<Order> Get() {
      return base.Get();
    }

  }

  public class EmployeesController : BaseController1<Employee, Int32> {

  }

  public class SuppliersController : BaseController1<Supplier, Int32> {

  }

  public class OrderDetailsController : BaseController2<OrderDetail, Int32, Int32> {

  }

  public class CategoriesController : BaseController1<Category, Int32> {

  }

  public class RegionsController : BaseController1<Region, Int32> {

  }

  public class TerritoriesController : BaseController1<Territory, Int32> {

  }


  public class UsersController : BaseController1<User, Int32> {

  }

  // OData WebApi 2.1 does not support enums - 2.2 is supposed to but wasn't released RTM as of 5/29/2014
  public class RolesController : BaseController<Role> {

  }

  public class CommentsController : BaseController1<Comment, Int32> {

  }

  public class TimeLimitsController : BaseController1<TimeLimit, Int32> {

  }


  

//    //[HttpGet]
//    //public String Metadata() {
//    //  var folder = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data");
//    //  var fileName = Path.Combine(folder, "metadata.json");
//    //  var jsonMetadata = File.ReadAllText(fileName);
//    //  return jsonMetadata;
//    //}

//    //[HttpGet]
//    //public HttpResponseMessage Metadata() {
//    //  var result = new HttpResponseMessage { Content = new StringContent(ContextProvider.Metadata())};
//    //  result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
//    //  return result;
//    //}

//    #region Save methods
////    [HttpPost]
////    public SaveResult SaveChanges(JObject saveBundle) {
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveWithTransactionScope(JObject saveBundle) {
////      var txSettings = new TransactionSettings() { TransactionType = TransactionType.TransactionScope };
////      return ContextProvider.SaveChanges(saveBundle, txSettings);
////    }

////    [HttpPost]
////    public SaveResult SaveWithDbTransaction(JObject saveBundle) {
////      var txSettings = new TransactionSettings() { TransactionType = TransactionType.DbTransaction };
////      return ContextProvider.SaveChanges(saveBundle, txSettings);
////    }

////    [HttpPost]
////    public SaveResult SaveWithNoTransaction(JObject saveBundle) {
////      var txSettings = new TransactionSettings() { TransactionType = TransactionType.None };
////      return ContextProvider.SaveChanges(saveBundle, txSettings);
////    }

////    [HttpPost]
////    public SaveResult SaveWithComment(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntitiesDelegate = AddComment;
////      return ContextProvider.SaveChanges(saveBundle);
////    }


////    [HttpPost]
////    public SaveResult SaveWithExit(JObject saveBundle) {
////      return new SaveResult() { Entities = new List<Object>(), KeyMappings = new List<KeyMapping>() };
////    }

////    [HttpPost]
////    public SaveResult SaveAndThrow(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntitiesDelegate = ThrowError;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveWithEntityErrorsException(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntitiesDelegate = ThrowEntityErrorsException;
////      return ContextProvider.SaveChanges(saveBundle);
////    }


////    [HttpPost]
////    public SaveResult SaveWithFreight(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntityDelegate = CheckFreight;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveWithFreight2(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntitiesDelegate = CheckFreightOnOrders;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveCheckInitializer(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntitiesDelegate = AddOrder;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveCheckUnmappedProperty(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntityDelegate = CheckUnmappedProperty;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveCheckUnmappedPropertySerialized(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntityDelegate = CheckUnmappedPropertySerialized;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    [HttpPost]
////    public SaveResult SaveCheckUnmappedPropertySuppressed(JObject saveBundle) {
////      ContextProvider.BeforeSaveEntityDelegate = CheckUnmappedPropertySuppressed;
////      return ContextProvider.SaveChanges(saveBundle);
////    }

////    private Dictionary<Type, List<EntityInfo>> ThrowError(Dictionary<Type, List<EntityInfo>> saveMap) {
////      throw new Exception("Deliberately thrown exception");
////    }

////    private Dictionary<Type, List<EntityInfo>> ThrowEntityErrorsException(Dictionary<Type, List<EntityInfo>> saveMap) {
////      List<EntityInfo> orderInfos;
////      if (saveMap.TryGetValue(typeof(Order), out orderInfos)) {
////        var errors = orderInfos.Select(oi => {
////#if NHIBERNATE
////          return new EntityError() {
////            EntityTypeName = typeof(Order).FullName,
////            ErrorMessage = "Cannot save orders with this save method",
////            ErrorName = "WrongMethod",
////            KeyValues = new object[] { ((Order) oi.Entity).OrderID },
////            PropertyName = "OrderID"
////          };
////#else
////          return new EFEntityError(oi, "WrongMethod", "Cannot save orders with this save method", "OrderID");
////#endif
////        });
////        var ex =  new EntityErrorsException("test of custom exception message", errors);
////        // if you want to see a different error status code use this.
////        // ex.StatusCode = HttpStatusCode.Conflict; // Conflict = 409 ; default is Forbidden (403).
////        throw ex;
////      }
////      return saveMap;
////    }


////    private Dictionary<Type, List<EntityInfo>> AddOrder(Dictionary<Type, List<EntityInfo>> saveMap) {
////      var order = new Order();
////      order.OrderDate = DateTime.Today;
////      var ei = ContextProvider.CreateEntityInfo(order);
////      List<EntityInfo> orderInfos;
////      if (!saveMap.TryGetValue(typeof(Order), out orderInfos)) {
////        orderInfos = new List<EntityInfo>();
////        saveMap.Add(typeof(Order), orderInfos);
////      }
////      orderInfos.Add(ei);

////      return saveMap;
////    }

////    private Dictionary<Type, List<EntityInfo>> CheckFreightOnOrders(Dictionary<Type, List<EntityInfo>> saveMap) {
////      List<EntityInfo> entityInfos;
////      if (saveMap.TryGetValue(typeof(Order), out entityInfos)) {
////        foreach (var entityInfo in entityInfos) {
////          CheckFreight(entityInfo);
////        }
////      }

////      return saveMap;
////    }

////    private bool CheckFreight(EntityInfo entityInfo) {
////      if ((ContextProvider.SaveOptions.Tag as String) == "freight update") {
////        var order = entityInfo.Entity as Order;
////        order.Freight = order.Freight + 1;
////      } else if ((ContextProvider.SaveOptions.Tag as String) == "freight update-ov") {
////        var order = entityInfo.Entity as Order;
////        order.Freight = order.Freight + 1;
////        entityInfo.OriginalValuesMap["Freight"] = null;
////      } else if ((ContextProvider.SaveOptions.Tag as String) == "freight update-force") {
////        var order = entityInfo.Entity as Order;
////        order.Freight = order.Freight + 1;
////        entityInfo.ForceUpdate = true;
////      }
////      return true;
////    }

////    private Dictionary<Type, List<EntityInfo>> AddComment(Dictionary<Type, List<EntityInfo>> saveMap) {
////      var comment = new Comment();
////      var tag = ContextProvider.SaveOptions.Tag;
////      comment.Comment1 = (tag == null) ? "Generic comment" : tag.ToString();
////      comment.CreatedOn = DateTime.Now;
////      comment.SeqNum = 1;
////      var ei = ContextProvider.CreateEntityInfo(comment);
////      List<EntityInfo> commentInfos;
////      if (!saveMap.TryGetValue(typeof(Comment), out commentInfos)) {
////        commentInfos = new List<EntityInfo>();
////        saveMap.Add(typeof(Comment), commentInfos);
////      }
////      commentInfos.Add(ei);

////      return saveMap;
////    }

////    private bool CheckUnmappedProperty(EntityInfo entityInfo) {
////      var unmappedValue = entityInfo.UnmappedValuesMap["myUnmappedProperty"];
////      if ((String)unmappedValue != "anything22") {
////        throw new Exception("wrong value for unmapped property:  " + unmappedValue);
////      }
////      Customer cust = entityInfo.Entity as Customer;
////      return false;
////    }

////    private bool CheckUnmappedPropertySuppressed(EntityInfo entityInfo) {
////      if (entityInfo.UnmappedValuesMap != null) { 
////        throw new Exception("unmapped properties should have been suppressed");
////      }
////      return false;
////    }

////    private bool CheckUnmappedPropertySerialized(EntityInfo entityInfo) {
////      var unmappedValue = entityInfo.UnmappedValuesMap["myUnmappedProperty"];
////      if ((String)unmappedValue != "ANYTHING22") {
////        throw new Exception("wrong value for unmapped property:  " + unmappedValue);
////      }
////      var anotherOne = entityInfo.UnmappedValuesMap["anotherOne"];

////      if (((dynamic) anotherOne).z[5].foo.Value != 4) {
////        throw new Exception("wrong value for 'anotherOne.z[5].foo'");
////      }

////      if (((dynamic)anotherOne).extra.Value != 666) {
////        throw new Exception("wrong value for 'anotherOne.extra'");
////      }

////      Customer cust = entityInfo.Entity as Customer;
////      if (cust.CompanyName.ToUpper() != cust.CompanyName) {
////        throw new Exception("Uppercasing of company name did not occur");
////      }
////      return false;
////    }

//    #endregion

//    #region standard queries

//    [HttpGet]
//    public List<Employee> QueryInvolvingMultipleEntities() {
//#if NHIBERNATE
//        // need to figure out what to do here
//        //return new List<Employee>();
//        var dc0 = new NorthwindNHContext();
//        var dc = new NorthwindNHContext();
//#elif CODEFIRST_PROVIDER
//        var dc0 = new NorthwindIBContext_CF();
//        var dc = new EFContextProvider<NorthwindIBContext_CF>();
//#elif DATABASEFIRST_OLD
//        var dc0 = new NorthwindIBContext_EDMX();
//        var dc = new EFContextProvider<NorthwindIBContext_EDMX>();
//#elif DATABASEFIRST_NEW
//      var dc0 = new NorthwindIBContext_EDMX_2012();
//      var dc = new EFContextProvider<NorthwindIBContext_EDMX_2012>();
//#elif ORACLE_EDMX
//      var dc0 = new NorthwindIBContext_EDMX_Oracle();
//      var dc = new EFContextProvider<NorthwindIBContext_EDMX_Oracle>();
//#endif
//      //the query executes using pure EF 
//      var query0 = (from t1 in dc0.Employees
//                    where (from t2 in dc0.Orders select t2.EmployeeID).Distinct().Contains(t1.EmployeeID)
//                    select t1);
//      var result0 = query0.ToList();

//      //the same query fails if using EFContextProvider
//      dc0 = dc.Context;
//      var query = (from t1 in dc0.Employees
//                   where (from t2 in dc0.Orders select t2.EmployeeID).Distinct().Contains(t1.EmployeeID)
//                   select t1);
//      var result = query.ToList();
//      return result;
//    }

//    [HttpGet]
////#if NHIBERNATE
////    [BreezeNHQueryable(MaxAnyAllExpressionDepth = 3)]
////#else
////    [EnableBreezeQuery(MaxAnyAllExpressionDepth = 3)]
////#endif
//    public IQueryable<Customer> Customers() {
//      var custs = _context.Customers;
//      return custs;
//    }

//    [HttpGet]
//    public IQueryable<Customer> CustomersStartingWith(string companyName) {
//      if (companyName == "null") {
//        throw new Exception("nulls should not be passed as 'null'");
//      }
//      if (String.IsNullOrEmpty(companyName)) {
//        companyName = "";
//      }
//      var custs = _context.Customers.Where(c => c.CompanyName.StartsWith(companyName));
//      return custs;
//    }

//    [HttpGet]
//    public Object CustomerCountsByCountry() {
//      return _context.Customers.GroupBy(c => c.Country).Select(g => new { g.Key, Count = g.Count() });
//    }


//    [HttpGet]
//    public Customer CustomerWithScalarResult() {
//      return _context.Customers.First();
//    }

//    [HttpGet]
//    public IQueryable<Customer> CustomersWithHttpError() {
//      var responseMsg = new HttpResponseMessage(HttpStatusCode.NotFound);
//      responseMsg.Content = new StringContent("Custom error message");
//      responseMsg.ReasonPhrase = "Custom Reason";
//      throw new HttpResponseException(responseMsg);
//    }

//    [HttpGet]
////#if NHIBERNATE
////    [BreezeNHQueryable(MaxExpansionDepth = 3)]
////#else
////    [EnableBreezeQuery(MaxExpansionDepth = 3)]
////#endif
//    public IQueryable<Order> Orders() {
//      var orders = _context.Orders;
//      return orders;
//    }

//    [HttpGet]
//    public IQueryable<Employee> Employees() {
//      return _context.Employees;
//    }

//    [HttpGet]
//    //[EnableBreezeQuery]
//    public IEnumerable<Employee> EnumerableEmployees() {
//      return _context.Employees.ToList();
//    }

//    [HttpGet]
//    public IQueryable<Employee> EmployeesFilteredByCountryAndBirthdate(DateTime birthDate, string country) {
//      return _context.Employees.Where(emp => emp.BirthDate >= birthDate && emp.Country == country);
//    }

//    [HttpGet]
//    public IQueryable<OrderDetail> OrderDetails() {
//      return _context.OrderDetails;
//    }

//    [HttpGet]
//    [Queryable]
//    public IQueryable<Product> Products() {
//      return _context.Products;
//    }

//    [HttpGet]
//    public IQueryable<Supplier> Suppliers() {
//      return _context.Suppliers;
//    }

//    [HttpGet]
//    public IQueryable<Region> Regions() {
//      return _context.Regions;
//    }


//    [HttpGet]
//    public IQueryable<Territory> Territories() {
//      return _context.Territories;
//    }

//    [HttpGet]
//    public IQueryable<Category> Categories() {
//      return _context.Categories;
//    }

//    [HttpGet]
//    public IQueryable<Role> Roles() {
//      return _context.Roles;
//    }

//    [HttpGet]
//    public IQueryable<User> Users() {
//      return _context.Users;
//    }

//    [HttpGet]
//    public IQueryable<TimeLimit> TimeLimits() {
//      return _context.TimeLimits;
//    }

//    [HttpGet]
//    public IQueryable<TimeGroup> TimeGroups() {
//      return _context.TimeGroups;
//    }

//    [HttpGet]
//    public IQueryable<Comment> Comments() {
//      return _context.Comments;
//    }

//    [HttpGet]
//    public IQueryable<UnusualDate> UnusualDates() {
//      return _context.UnusualDates;
//    }

//#if ! DATABASEFIRST_OLD
//    [HttpGet]
//    public IQueryable<Geospatial> Geospatials() {
//      return _context.Geospatials;
//    }
//#endif

//    #endregion

//    #region named queries

//    [HttpGet]
//    public Customer CustomerFirstOrDefault() {
//      var customer = _context.Customers.Where(c => c.CompanyName.StartsWith("blah")).FirstOrDefault();
//      return customer;
//    }

//    [HttpGet]
//    // AltCustomers will not be in the resourceName/entityType map;
//    public IQueryable<Customer> AltCustomers() {
//      return _context.Customers;
//    }


//    [HttpGet]
//    public IQueryable<Employee> SearchEmployees([FromUri] int[] employeeIds) {
//      var query = _context.Employees.AsQueryable();
//      if (employeeIds.Length > 0) {
//        query = query.Where(emp => employeeIds.Contains(emp.EmployeeID));
//        var result = query.ToList();
//      }
//      return query;
//    }

//    [HttpGet]
//    public IQueryable<Customer> SearchCustomers([FromUri] CustomerQBE qbe) {
//      // var query = _context.Customers.Where(c =>
//      //    c.CompanyName.StartsWith(qbe.CompanyName));
//      var ok = qbe != null && qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1;
//      if (!ok) {
//        throw new Exception("qbe error");
//      }
//      // just testing that qbe actually made it in not attempted to write qbe logic here
//      // so just return first 3 customers.
//      return _context.Customers.Take(3);
//    }

//    [HttpGet]
//    public IQueryable<Customer> SearchCustomers2([FromUri] CustomerQBE[] qbeList) {

//      if (qbeList.Length < 2) {
//        throw new Exception("all least two items must be passed in");
//      }
//      var ok = qbeList.All(qbe => {
//        return qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1;
//      });
//      if (!ok) {
//        throw new Exception("qbeList error");
//      }
//      // just testing that qbe actually made it in not attempted to write qbe logic here
//      // so just return first 3 customers.
//      return _context.Customers.Take(3);
//    }

//    public class CustomerQBE {
//      public String CompanyName { get; set; }
//      public String[] ContactNames { get; set; }
//      public String City { get; set; }
//    }

//    [HttpGet]
//    public IQueryable<Customer> CustomersOrderedStartingWith(string companyName) {
//      var customers = _context.Customers.Where(c => c.CompanyName.StartsWith(companyName)).OrderBy(cust => cust.CompanyName);
//      var list = customers.ToList();
//      return customers;
//    }

//    [HttpGet]
//    public IQueryable<Employee> EmployeesMultipleParams(int employeeID, string city) {
//      // HACK: 
//      if (city == "null") {
//        city = null;
//      }
//      var emps = _context.Employees.Where(emp => emp.EmployeeID == employeeID || emp.City.Equals(city));
//      return emps;
//    }

//    [HttpGet]
//    public IEnumerable<Object> Lookup1Array() {
//      var regions = _context.Regions;

//      var lookups = new List<Object>();
//      lookups.Add(new {regions = regions});
//      return lookups;
//    }

//    [HttpGet]
//    public object Lookups() {
//      var regions = _context.Regions;
//      var territories = _context.Territories;
//      var categories = _context.Categories;

//      var lookups = new { regions, territories, categories };
//      return lookups;
//    }

//    [HttpGet]
//    public IEnumerable<Object> LookupsEnumerableAnon() {
//      var regions = _context.Regions;
//      var territories = _context.Territories;
//      var categories = _context.Categories;

//      var lookups = new List<Object>();
//      lookups.Add(new {regions = regions, territories = territories, categories = categories});
//      return lookups;
//    }

//    [HttpGet]
//    public IQueryable<Object> CompanyNames() {
//      var stuff = _context.Customers.Select(c => c.CompanyName);
//      return stuff;
//    }

//    [HttpGet]
//    public IQueryable<Object> CompanyNamesAndIds() {
//      var stuff = _context.Customers.Select(c => new { c.CompanyName, c.CustomerID });
//      return stuff;
//    }

//    [HttpGet]
//    public IQueryable<CustomerDTO> CompanyNamesAndIdsAsDTO() {
//      var stuff = _context.Customers.Select(c => new CustomerDTO() { CompanyName = c.CompanyName, CustomerID = c.CustomerID });
//      return stuff;
//    }

//    public class CustomerDTO {
//      public CustomerDTO() {
//      }

//      public CustomerDTO(String companyName, Guid customerID) {
//        CompanyName = companyName;
//        CustomerID = customerID;
//      }
      
//      public Guid CustomerID { get; set; }
//      public String CompanyName { get; set; }
//      public AnotherType AnotherItem { get; set; }
//    }

//    public class AnotherType {
      
//    }


//    [HttpGet]
//    public IQueryable<Object> CustomersWithBigOrders() {
//      var stuff = _context.Customers.Where(c => c.Orders.Any(o => o.Freight > 100)).Select(c => new { Customer = c, BigOrders = c.Orders.Where(o => o.Freight > 100) });
//      return stuff;
//    }



//    [HttpGet]
//#if NHIBERNATE
//    public IQueryable<Object> CompanyInfoAndOrders(System.Web.Http.OData.Query.ODataQueryOptions options) {
//        // Need to handle this specially for NH, to prevent $top being applied to Orders
//        var query = _context.Customers;
//        var queryHelper = new NHQueryHelper();

//        // apply the $filter, $skip, $top to the query
//        var query2 = queryHelper.ApplyQuery(query, options);

//        // execute query, then expand the Orders
//        var r = query2.Cast<Customer>().ToList();
//        NHInitializer.InitializeList(r, "Orders");

//        // after all is loaded, create the projection
//        var stuff = r.AsQueryable().Select(c => new { c.CompanyName, c.CustomerID, c.Orders });
//        queryHelper.ConfigureFormatter(Request, query);
//#else
//    public IQueryable<Object> CompanyInfoAndOrders() {
//      var stuff = _context.Customers.Select(c => new { c.CompanyName, c.CustomerID, c.Orders });
//#endif
//      return stuff;
//    }

//    [HttpGet]
//    public Object CustomersAndProducts() {
//      var stuff = new { Customers = _context.Customers.ToList(), Products = _context.Products.ToList() };
//      return stuff;
//    }

//    [HttpGet]
//    public IQueryable<Object> TypeEnvelopes() {
//      var stuff = this.GetType().Assembly.GetTypes()
//        .Select(t => new { t.Assembly.FullName, t.Name, t.Namespace })
//        .AsQueryable();
//      return stuff;
//    }


//    [HttpGet]
//    public IQueryable<Customer> CustomersAndOrders() {
//      var custs = _context.Customers.Include("Orders");
//      return custs;
//    }

//    [HttpGet]
//    public IQueryable<Order> OrdersAndCustomers() {
//      var orders = _context.Orders.Include("Customer");
//      return orders;
//    }


//    [HttpGet]
//    public IQueryable<Customer> CustomersStartingWithA() {
//      var custs = _context.Customers.Where(c => c.CompanyName.StartsWith("A"));
//      return custs;
//    }

//    [HttpGet]
////#if NHIBERNATE
////    [BreezeNHQueryable]
////#else
////    [EnableBreezeQuery]
////#endif
//    public HttpResponseMessage CustomersAsHRM() {
//      var customers = _context.Customers.Cast<Customer>();
//      var response = Request.CreateResponse(HttpStatusCode.OK, customers);
//      return response;
//    }

//    #endregion

//  }

    
}