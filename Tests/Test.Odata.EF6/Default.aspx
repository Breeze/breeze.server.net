<!DOCTYPE html>
<html lang="en">
    <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width" />
        <meta http-equiv="X-UA-Compatible" content="IE=edge, chrome=1"/>
        <title id="title">Breeze OData Test Suite</title>
       
        <link rel="stylesheet" href="Scripts/libs/qunit-1.11.0.css" type="text/css" media="screen"/>
        <script type="text/javascript" src="Scripts/libs/modernizr-2.0.6-development-only.js" ></script>
        
    </head>

    <body>
        <div id="qunit"></div>
        <div id="qunit-fixture"></div>
        <div id="test-dev"></div>
    </body>
    
    <!-- load 3rd party libs -->
    <script src="Scripts/libs/qunit-1.11.0.js"></script> 
    <script src="Scripts/libs/q.min.js"></script>
    <script src="Scripts/libs/knockout-2.3.0.debug.js"></script>
    <script src="Scripts/libs/underscore.js"></script>
    <script src="Scripts/libs/backbone.js"></script>
    <script src="Scripts/libs/jquery-2.0.3.js"></script>
    <script src="Scripts/libs/datajs-1.1.1.js"></script>
 
    <!-- Test helper scripts  --> 
    <!-- These two must come first -->    
    <script src="/Scripts/src/breeze.debug.js"></script>
    <script src="/Scripts/tests/testFns.js"></script>  
    
    <script>
        // -- Initialize Test vars  
        breezeTestFns.setDataService("OData");
    </script>
       
    <script src="/Scripts/tests/attachTests.js"></script> 
    <script src="/Scripts/tests/classRewriteTests.js"></script> 
    <script src="/Scripts/tests/complexTypeTests.js"></script> 
    <script src="/Scripts/tests/entityManagerTests.js"></script> 
    <script src="/Scripts/tests/entityTests.js"></script> 
    <script src="/Scripts/tests/inheritBillingTests.js"></script> 
    <script src="/Scripts/tests/inheritProduceTests.js"></script> 
    <script src="/Scripts/tests/koSpecificTests.js"></script> 
    <script src="/Scripts/tests/metadataTests.js"></script> 
    <script src="/Scripts/tests/miscTests.js"></script> 
    <script src="/Scripts/tests/paramTests.js"></script> 
    <script src="/Scripts/tests/queryTests.js"></script> 
    <script src="/Scripts/tests/queryCtorTests.js"></script> 
    <script src="/Scripts/tests/queryDatatypeTests.js"></script> 
    <script src="/Scripts/tests/queryLocalTests.js"></script> 
    <script src="/Scripts/tests/queryNamedTests.js"></script> 
    <script src="/Scripts/tests/queryNonEFTests.js"></script> 
    <script src="/Scripts/tests/queryRawOdataTests.js"></script> 
    <script src="/Scripts/tests/querySelectTests.js"></script> 
    <script src="/Scripts/tests/saveTests.js"></script> 
    <script src="/Scripts/tests/validateTests.js"></script> 
    <script src="/Scripts/tests/validateEntityTests.js"></script> 
      
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
            //        require.config({ baseUrl: "Scripts/tests" });
            //        require([module], loadNext);
            //    } else {
            //        QUnit.start();
            //    }
            //}
            //loadNext();
        }
       

    </script>
</html>
