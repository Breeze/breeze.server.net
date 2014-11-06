using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Sample_WebApi2.Controllers
{
    public class TestConfigController : ApiController
    {
        // GET api/<controller>
        public IDictionary<string, object> Get()
        {
            var map = new Dictionary<string, object>();
            map.Add("value", "WebApi");
            map.Add("version", NorthwindContextProvider.CONFIG_VERSION);
            return map;
        }
    }
}