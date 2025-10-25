using Breeze.AspNetCore;
using Breeze.Persistence;
using Foo;
using Microsoft.AspNetCore.Mvc;

namespace TestBreeze {
  /// <summary>
  /// Tests for query string handling with BreezeConfig.QueryParamName.
  /// </summary>
  [TestClass]
  public sealed class TestQueryString {

    [TestMethod]
    public void TestTake() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.AsQueryable();

      var qs = "?{\"take\":3}";
      var aec = Util.NewActionExecutedContext(qs, customers);

      var filter = new BreezeQueryFilterAttribute();
      filter.OnActionExecuted(aec);

      var rows = Util.AssertQueryResult<Customer>(aec.Result);
      Assert.IsTrue(rows.Count() == 3);
      var row0 = rows[0];
      Assert.IsTrue(row0.City != null);
    }

    [TestMethod]
    public void TestQueries() {
      var queryable = new List<Customer>().AsQueryable();

      var aec = Util.NewActionExecutedContext("", queryable);
      var r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual(null, r);

      aec = Util.NewActionExecutedContext("?x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual(null, r);

      aec = Util.NewActionExecutedContext("?{}&x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual(null, r);

      aec = Util.NewActionExecutedContext("?{\"take\":3}", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual("{\"take\":3}", r);

      aec = Util.NewActionExecutedContext("?{\"take\":3}&x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual("{\"take\":3}", r);

      aec = Util.NewActionExecutedContext("?{\"where\":{\"name\":{\"eq\":\"sam%20%26%20ray\"}}&x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual("{\"where\":{\"name\":{\"eq\":\"sam & ray\"}}", r);
    }

    [TestMethod]
    public void TestObject() {

      var data = new { name = "Bob" };
      var qs = "";
      var aec = Util.NewActionExecutedContext(qs, new ObjectResult(data));

      var filter = new BreezeQueryFilterAttribute { MaxDepth = 2, MaxTake = 3 };
      filter.OnActionExecuted(aec);

      var res = Util.AssertObjectResult(aec.Result);
      Assert.IsInstanceOfType(res, typeof(object));
    }

    [TestMethod]
    public void TestEmpty() {

      //var qs = "";
      var aec = Util.NewActionExecutedContext(null, new StatusCodeResult(200));

      var filter = new BreezeQueryFilterAttribute { MaxDepth = 2, MaxTake = 3 };
      filter.OnActionExecuted(aec);

      Assert.IsInstanceOfType<StatusCodeResult>(aec.Result);
    }


    [TestMethod]
    public void TestString() {

      var qs = "?{\"parameters\":{\"groupId\":28,\"isStaff\":false}}";
      var aec = Util.NewActionExecutedContext(qs, new ObjectResult("hello"));

      var filter = new BreezeQueryFilterAttribute { MaxDepth = 2, MaxTake = 3 };
      filter.OnActionExecuted(aec);

      var res = Util.AssertObjectResult(aec.Result);
      Assert.IsInstanceOfType(res, typeof(string));
    }

    [TestMethod]
    public void TestList() {
      
      var data = new List<string> { "one", "two", "three", "four" };
      var qs = "?{\"parameters\":{\"groupId\":28,\"isStaff\":false}}";
      var aec = Util.NewActionExecutedContext(qs, new ObjectResult(data));

      var filter = new BreezeQueryFilterAttribute { MaxDepth = 2, MaxTake = 3 };
      filter.OnActionExecuted(aec);

      // make sure that filter does not turn List into QueryResult
      var res = Util.AssertObjectResult(aec.Result);
      Assert.IsInstanceOfType(res, typeof(List<string>));
    }

    [TestMethod]
    public async Task TestDictionaryAsync() {

      var data = new Dictionary<string, object>() { { "a", "111" }, { "b", "222" } };
      var qs = "?{\"parameters\":{\"groupId\":28,\"isStaff\":false}}";
      var aec = Util.NewActionExecutedContext(qs, new ObjectResult(data));

      var filter = new BreezeAsyncQueryFilterAttribute() { MaxDepth = 2, MaxTake = 3 };
      await filter.OnActionExecutionAsync(Util.NewActionExecutingContext(), Util.GetNextDelegate(aec));

      var res = Util.AssertObjectResult(aec.Result);
      Assert.IsInstanceOfType(res, typeof(Dictionary<string, object>));
    }

  }
}
