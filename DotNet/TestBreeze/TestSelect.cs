using Breeze.AspNetCore;
using Breeze.Core;
using Breeze.Persistence.EFCore;
using Microsoft.CSharp.RuntimeBinder;

namespace TestBreeze {
  /// <summary>
  /// Tests for Select clause handling in BreezeQueryFilter and BreezeAsyncQueryFilter, with MaxDepth parameter.
  /// </summary>
  [TestClass]
  public sealed class TestSelect {
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context) {
      // This method is called once for the test assembly, before any tests are run.
      // These assignments are normally done by EFPersistenceManager
      EntityQuery.ApplyExpand = EFExtensions.ApplyExpand;
      EntityQuery.ApplyCustomLogic = EFExtensions.ApplyAsNoTracking;
    }

    [ClassInitialize]
    public static void ClassInit(TestContext context) {
      // This method is called once for the test class, before any tests of the class are run.
    }

    [TestInitialize]
    public void TestInit() {
      // This method is called before each test method.
    }

    [TestMethod]
    public void TestSelect1() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"CompanyName\",\"City\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      // next 2 lines are different between sync and async query filter
      var filter = new BreezeQueryFilterAttribute();
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<object>(aec.Result);
      Assert.IsTrue(rows.Count == 10);
      var row0 = (dynamic) rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.CompanyName != null);
      // projection does not have CustomerID
      Assert.ThrowsException<RuntimeBinderException>(() => row0.CustomerID);
    }

    [TestMethod]
    public async Task TestSelect1Async() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"CompanyName\",\"City\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      // next 2 lines are different between sync and async query filter
      var filter = new BreezeAsyncQueryFilterAttribute();
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<object>(aec.Result);
      Assert.IsTrue(rows.Count == 10);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.CompanyName != null);
      // projection does not have CustomerID
      Assert.ThrowsException<RuntimeBinderException>(() => row0.CustomerID);
    }

    [TestMethod]
    public void TestSelect2() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Orders.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Customer.CompanyName\",\"ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 1 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<object>(aec.Result);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.ShipCity != null);
      Assert.IsTrue(row0.Customer_CompanyName != null);
      // projection does not have OrderID
      Assert.ThrowsException<RuntimeBinderException>(() => row0.OrderID);
    }

    [TestMethod]
    public async Task TestSelect2Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Orders.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Customer.CompanyName\",\"ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 1 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<object>(aec.Result);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.ShipCity != null);
      Assert.IsTrue(row0.Customer_CompanyName != null);
      // projection does not have OrderID
      Assert.ThrowsException<RuntimeBinderException>(() => row0.OrderID);
    }

    [TestMethod]
    public void TestSelectMax1() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Orders.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Customer.CompanyName\",\"ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 0 };
      filter.OnActionExecuted(aec);

      Util.AssertBadRequest(aec.Result, "MaxDepth");
    }

    [TestMethod]
    public async Task TestSelectMax1Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Orders.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Customer.CompanyName\",\"ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 0 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      Util.AssertBadRequest(aec.Result, "MaxDepth");
    }

    [TestMethod]
    public void TestSelectMax2() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.OrderDetails.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Order.Employee.LastName\",\"Order.ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 1 };
      filter.OnActionExecuted(aec);

      Util.AssertBadRequest(aec.Result, "MaxDepth");
    }

    [TestMethod]
    public async Task TestSelectMax2Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.OrderDetails.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Order.Employee.LastName\",\"Order.ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 1 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      Util.AssertBadRequest(aec.Result, "MaxDepth");
    }

    [TestMethod]
    public void TestSelectMax3() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.OrderDetails.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Order.Employee.LastName\",\"Order.ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 2 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<object>(aec.Result);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.Order_ShipCity != null);
      Assert.IsTrue(row0.Order_Employee_LastName != null);
    }

    [TestMethod]
    public async Task TestSelectMax3Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.OrderDetails.Take(10).AsQueryable();

      var qs = "?{\"select\":[\"Order.Employee.LastName\",\"Order.ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 2 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<object>(aec.Result);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.Order_ShipCity != null);
      Assert.IsTrue(row0.Order_Employee_LastName != null);
    }

    [TestMethod]
    public void TestSelectMax4() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.OrderDetails.Select(x => new { x.Order.Employee.LastName, x.Order.ShipCity });

      //var qs = "?{\"select\":[\"Order.Employee.LastName\",\"Order.ShipCity\"]}";
      var qs = "";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 2, MaxTake = 10 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<object>(aec.Result);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.ShipCity != null);
      Assert.IsTrue(row0.LastName != null);
    }

    [TestMethod]
    public async Task TestSelectMax4Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.OrderDetails.Take(20).Select(x => new { x.Order.Employee.LastName, x.Order.ShipCity });

      //var qs = "?{\"select\":[\"Order.Employee.LastName\",\"Order.ShipCity\"]}";
      var qs = "";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 2, MaxTake = 10 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<object>(aec.Result);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.ShipCity != null);
      Assert.IsTrue(row0.LastName != null);
    }

  }
}
