testApp = window.testApp || {};
testApp.dataservice = (function (breeze, testFns) {

  var em;
  QUnit.config.testTimeout = 300000; // 5 minutes

  return {
    queryOrderDetails: queryOrderDetails
  };

  function queryOrderDetails(multiple, expands) {
    var query = breeze.EntityQuery.from("OrderDetailsMultiple")
        .withParameters({ multiple: multiple, expands: expands });

    return getEm().executeQuery(query);
  }

  function getEm() {
    if (!em) {
      testFns.setup({ serviceName: "/breeze/NorthwindIBModel" });
      em = testFns.newEm();
    }
    return em;
  }

})(breeze, breezeTestFns);