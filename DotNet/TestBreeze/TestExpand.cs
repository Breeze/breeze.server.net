using Breeze.AspNetCore;
using Foo;

namespace TestBreeze {
  /// <summary>
  /// Tests for Expand clause handling in BreezeQueryFilter and BreezeAsyncQueryFilter, with MaxDepth parameter.
  /// </summary>
  [TestClass]
  public sealed class TestExpand {

    [TestMethod]
    public void TestExpand1() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"take\":1,\"expand\":[\"Orders\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeQueryFilterAttribute();
      filter.OnActionExecuted(aec);

      var rows = Util.AssertListResult<Customer>(aec.Result);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.Orders.Count > 0);
    }

    [TestMethod]
    public async Task TestExpand1Async() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"take\":1,\"expand\":[\"Orders\"]}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeAsyncQueryFilterAttribute();
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertListResult<Customer>(aec.Result);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
      Assert.IsTrue(row0.Orders.Count > 0);
    }

    [TestMethod]
    public void TestExpandMax1() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"expand\":[\"Orders.OrderDetails\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 2 };
      filter.OnActionExecuted(aec);

      var rows = Util.AssertListResult<Customer>(aec.Result);
      var row0 = rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.Orders.Count > 0);
      Assert.IsTrue(row0.Orders.First().OrderDetails.Count > 0);
    }

    [TestMethod]
    public async Task TestExpandMax1Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"expand\":[\"Orders.OrderDetails\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 2 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var rows = Util.AssertListResult<Customer>(aec.Result);
      var row0 = rows[0];
      Assert.IsNotNull(row0);
      Assert.IsTrue(row0.Orders.Count > 0);
      Assert.IsTrue(row0.Orders.First().OrderDetails.Count > 0);
    }

    [TestMethod]
    public void TestExpandMax2() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"expand\":[\"Orders.OrderDetails\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeQueryFilterAttribute() { MaxDepth = 1 };
      filter.OnActionExecuted(aec);

      Util.AssertBadRequest(aec.Result, "MaxDepth");
    }

    [TestMethod]
    public async Task TestExpandMax2Async() {
      var dbx = Util.NorthwindIB();
      var orders = dbx.Customers.Take(10).AsQueryable();

      var qs = "?{\"expand\":[\"Orders.OrderDetails\"]}";
      var aec = Util.NewActionExecutedContext(qs, orders);

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 1 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      Util.AssertBadRequest(aec.Result, "MaxDepth");
    }

  }
}
