using Breeze.AspNetCore;
using Foo;

namespace TestBreeze {
  /// <summary>
  /// Tests for Take clause handling in BreezeQueryFilter and BreezeAsyncQueryFilter, with MaxTake parameter.
  /// </summary>
  [TestClass]
  public sealed class TestTake {

    [TestMethod]
    public void TestTake1() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(15).AsQueryable();

      var qs = "?{\"take\":10,\"expand\":[\"Orders\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeQueryFilterAttribute();
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      // prove that smallest Take wins
      Assert.AreEqual(10, rows.Count);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.Orders.Count > 0);
    }

    [TestMethod]
    public async Task TestTake1Async() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"take\":1,\"expand\":[\"Orders\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeAsyncQueryFilterAttribute();
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      Assert.AreEqual(1, rows.Count);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.Orders.Count > 0);
    }
    
    [TestMethod]
    public void TestTakeMax1() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(15).AsQueryable();

      var qs = "?{\"take\":10,\"expand\":[\"Orders\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeQueryFilterAttribute() { MaxTake = 5 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      // prove that smallest Take wins
      Assert.AreEqual(5, rows.Count);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.Orders.Count > 0);
    }

    [TestMethod]
    public async Task TestTakeMax1Async() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(15).AsQueryable();

      var qs = "?{\"take\":10,\"expand\":[\"Orders\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxTake = 5 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      // prove that smallest Take wins
      Assert.AreEqual(5, rows.Count);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.Orders.Count > 0);
    }

    [TestMethod]
    public void TestTakeMax2() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Customers.AsQueryable();

      var qs = "?{\"take\":5}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxTake = 10 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      // prove that smallest Take wins
      Assert.AreEqual(5, rows.Count);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
    }

    [TestMethod]
    public async Task TestTakeMax2Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Customers.AsQueryable();

      var qs = "?{\"take\":5}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxTake = 10 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      // prove that smallest Take wins
      Assert.AreEqual(5, rows.Count);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
    }

    [TestMethod]
    public void TestTakeMax3() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Orders.AsQueryable();

      var qs = "?{\"take\":10,\"select\":[\"Customer.CompanyName\",\"ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxTake = 5 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<object>(aec.Result);
      Assert.AreEqual(5, rows.Count);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.ShipCity != null);
      Assert.IsTrue(row0.Customer_CompanyName != null);
    }

    [TestMethod]
    public async Task TestTakeMax3Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Orders.AsQueryable();

      var qs = "?{\"take\":10,\"select\":[\"Customer.CompanyName\",\"ShipCity\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxTake = 5 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertQueryResult<object>(aec.Result);
      Assert.AreEqual(5, rows.Count);
      var row0 = (dynamic)rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.ShipCity != null);
      Assert.IsTrue(row0.Customer_CompanyName != null);
    }

  }
}
