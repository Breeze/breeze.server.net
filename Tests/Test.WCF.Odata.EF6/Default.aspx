<!DOCTYPE html>
<html lang="en">
    <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width" />
        <meta http-equiv="X-UA-Compatible" content="IE=edge, chrome=1"/>
        <title id="title">Breeze OData Test Suite</title>
       
        <link rel="stylesheet" href="vendor/qunit-1.11.0.css" type="text/css" media="screen"/>
        <script type="text/javascript" src="vendor/modernizr-2.0.6-development-only.js" ></script>
        
    </head>

    <body>
        <div id="qunit"></div>
        <div id="qunit-fixture"></div>
        <div id="test-dev"></div>
    </body>
    
    <!-- load 3rd party libs -->
    <script src="vendor/qunit-1.11.0.js"></script> 
    <script src="vendor/sinon.js"></script>
    <!-- DO NOT USE sinon-qunit. Breaks our tests for some reason. Don't need anyway-->
    <!--<script src="vendor/sinon-qunit.js"></script>-->
    <script src="vendor/q.min.js"></script>
    <script src="vendor/knockout-2.3.0.debug.js"></script>
    <script src="vendor/underscore.js"></script>
    <script src="vendor/backbone.js"></script>
    <script src="vendor/jquery-2.0.3.js"></script>
    <script src="vendor/datajs-1.1.1.js"></script>
 
    <!-- Test helper scripts  --> 
    <!-- These two must come first -->    
    <script src="breeze/breeze.debug.js"></script>
    <script src="tests/testFns.js"></script>  
    
    <script>
        // -- Initialize Test vars  
        breezeTestFns.setDataService("OData", "WCF");
    </script>
       
    <script src="tests/ajaxAdapterTests.js"></script>
    <script src="tests/attachTests.js"></script> 
    <script src="tests/classRewriteTests.js"></script> 
    <script src="tests/complexTypeTests.js"></script> 
    <script src="tests/entityManagerTests.js"></script> 
    <script src="tests/entityTests.js"></script> 
    <script src="tests/inheritBillingTests.js"></script> 
    <script src="tests/inheritProduceTests.js"></script> 
    <script src="tests/koSpecificTests.js"></script> 
    <script src="tests/metadataTests.js"></script> 
    <script src="tests/miscTests.js"></script> 
    <script src="tests/paramTests.js"></script> 
    <script src="tests/queryTests.js"></script> 
    <script src="tests/queryCtorTests.js"></script> 
    <script src="tests/queryDatatypeTests.js"></script> 
    <script src="tests/queryLocalTests.js"></script> 
    <script src="tests/queryNamedTests.js"></script> 
    <script src="tests/queryNonEFTests.js"></script> 
    <script src="tests/queryRawOdataTests.js"></script> 
    <script src="tests/querySelectTests.js"></script>
    <script src="tests/saveInterceptorTests.js"></script>     
    <script src="tests/saveTests.js"></script> 
    <script src="tests/validateTests.js"></script> 
    <script src="tests/validateEntityTests.js"></script> 
      
    <!-- Tests are loaded; let's go! -->
    <script>

        document.getElementById("title").innerHTML += " -> " + breezeTestFns.title;
        
        if (!QUnit.urlParams.sequential) {
            if (QUnit.urlParams.canStart) {
                QUnit.start(); //Tests loaded, run tests
            }
        } else {
            // Steve. - I wasn't sure how to put this back once I removed "require" dependency.
            //function loadNext() {
            //    var module = modules.shift();
            //    if (module) {
            //        require.config({ baseUrl: "tests" });
            //        require([module], loadNext);
            //    } else {
            //        QUnit.start();
            //    }
            //}
            //loadNext();
        }
       

    </script>
</html>
