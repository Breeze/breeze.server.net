using Breeze.AspNetCore;
using Breeze.Persistence;
using Foo;

namespace TestBreeze {
  /// <summary>
  /// Tests for query string handling with BreezeConfig.QueryParamName.
  /// </summary>
  [TestClass]
  public sealed class TestQueryStringNamed {

    [TestInitialize]
    public void TestInit() {
      BreezeConfig.Instance.QueryParamName = "bq";
    }

    [TestCleanup]
    public void TestCleanup() {
      BreezeConfig.Instance.QueryParamName = null;
    }


    [TestMethod]
    public void TestTake() {
      var dbx = Util.NorthwindIB();
      var customers = dbx.Customers.AsQueryable();

      var qs = "?bq={\"take\":3}";
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

      aec = Util.NewActionExecutedContext("?bq=", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual(null, r);

      aec = Util.NewActionExecutedContext("?bq={}&x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual(null, r);

      aec = Util.NewActionExecutedContext("?bq={\"take\":3}", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual("{\"take\":3}", r);

      aec = Util.NewActionExecutedContext("?bq={\"take\":3}&x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual("{\"take\":3}", r);

      aec = Util.NewActionExecutedContext("?bq={\"where\":{\"name\":{\"eq\":\"sam%20%26%20ray\"}}&x=12", queryable);
      r = QueryFns.ExtractAndDecodeQueryString(aec);
      Assert.AreEqual("{\"where\":{\"name\":{\"eq\":\"sam & ray\"}}", r);
    }
  }
}
