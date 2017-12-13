using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Breeze.AspNetCore;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class MetadataTestController : Controller {


    //  [HttpGet]
    //  public String Metadata() {
    //    var folder = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data");
    //    var fileName = Path.Combine(folder, "metadataTest.json");
    //    var jsonMetadata =  System.IO.File.ReadAllText(fileName);
    //    return jsonMetadata;
    //  }


  }
}