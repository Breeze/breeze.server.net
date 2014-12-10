testApp = window.testApp || {};
testApp.dataservice = (function (breeze, testFns) {

  var em;
  QUnit.config.testTimeout = 300000; // 5 minutes

  return {
    queryOrderDetails: queryOrderDetails,
    getEmEntityCount: getEmEntityCount,
    clearEm: clearEm
  };

  function queryOrderDetails(multiple, expands) {
    var query = breeze.EntityQuery.from("OrderDetailsMultiple")
        .withParameters({ multiple: multiple, expands: expands });

    return getEm().executeQuery(query);
  }

  function getEmEntityCount() {
    return getEm().getEntities().length;
  }

  function clearEm() {
    return getEm().clear();
  }

  function getEm() {
    if (!em) {
      testFns.setup({ serviceName: "/breeze/NorthwindIBModel" });
      em = testFns.newEm();
    }
    return em;
  }

})(breeze, breezeTestFns);