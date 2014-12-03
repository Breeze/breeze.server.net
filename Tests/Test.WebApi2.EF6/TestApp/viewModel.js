testApp = window.testApp || {};
testApp.viewModel = (function (dataservice) {

  return {
    loadOrderDetails: loadOrderDetails
  };

  function loadOrderDetails() {
    var multiple = document.getElementById("multiple").value;
    var expands = document.getElementById("expands").value;
    document.getElementById("entityCount").value = '';
    dataservice.queryOrderDetails(multiple, expands).then(function (data) {
      var count = data.retrievedEntities.length;
      document.getElementById("entityCount").value = count;
    });
  }

})(testApp.dataservice);