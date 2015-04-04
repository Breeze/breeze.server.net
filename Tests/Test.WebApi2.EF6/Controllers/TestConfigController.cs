using Breeze.WebApi2;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Http;

namespace Sample_WebApi2.Controllers
{
    [BreezeController]
    public class TestConfigController : ApiController
    {
        // GET api/<controller>
        public IDictionary<string, object> Get()
        {
            var map = new Dictionary<string, object>();
            map.Add("value", "DotNetWebApi");
            map.Add("version", NorthwindContextProvider.CONFIG_VERSION);
            map.Add("metadata", LoadMetadata());
            return map;
        }

        private string LoadMetadata()
        {
            var folder = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data");
            var configName = NorthwindContextProvider.CONFIG_VERSION.ToLower();
            var fileName = Path.Combine(folder, "metadata." + configName + ".json");
            var jsonMetadata = File.ReadAllText(fileName);
            return jsonMetadata;
        }
    }
}