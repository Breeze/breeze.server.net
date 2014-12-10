testApp = window.testApp || {};
testApp.viewModel = (function (dataservice) {

  var _totalRetrieved = 0;
  document.getElementById("entityCount").value = '0';
  document.getElementById("entitiesInCache").value = '0';

  return {
    loadOrderDetails: loadOrderDetails,
    clearEm: clearEm
  };

  function loadOrderDetails() {
    var iterations = document.getElementById("iterations").value || 1;
    var multiple = document.getElementById("multiple").value || 1;
    var expands = document.getElementById("expands").value || "";
    setStatus("query in progress");
    executeQuery(multiple, expands, iterations).then(function () {
      showCounts("all done");
    }).fail(function (err) {
      showCounts("Error:" + err);
      // console.error(err);
    });
  }

  function clearEm() {
    dataservice.clearEm();
    showCounts("entityManager cleared");
  }

  function executeQuery(multiple, expands, iterations) {
    return dataservice.queryOrderDetails(multiple, expands).then(function (data) {
      var count = data.retrievedEntities.length;
      _totalRetrieved = _totalRetrieved + count;
      var status = iterations > 1 ? "iterations remaining: " + (iterations-1) : "all queries complete";
      showCounts(status);
      if (iterations > 1) {
        return executeQuery(multiple, expands, iterations - 1);
      }
    });
  }

  function showCounts(status) {
    setStatus(status);
    document.getElementById("entitiesInCache").value = dataservice.getEmEntityCount();
    document.getElementById("entityCount").value = _totalRetrieved;
  }

  function setStatus(status) {
    document.getElementById("status").innerHTML = status || "";
  }

})(testApp.dataservice);