using Breeze.AspNetCore.EFCore.Sample.Data;
using Breeze.AspNetCore.EFCore.Sample.Models;
using Breeze.Persistence;
using Breeze.Persistence.EFCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Breeze.AspNetCore.EFCore.Sample.Controllers
{
    [Route("breeze/[controller]/[action]")]
    [BreezeQueryFilter]
    public class SchoolModelController : Controller
    {
        private SchoolPersistenceManager PersistenceManager;

        // called via DI 
        public SchoolModelController(SchoolContext context)
        {
            PersistenceManager = new SchoolPersistenceManager(context);
        }

        [HttpGet]
        public IActionResult Metadata()
        {
            return Ok(PersistenceManager.Metadata());
        }
        [HttpPost]
        public SaveResult SaveChanges([FromBody] JObject saveBundle)
        {
            return PersistenceManager.SaveChanges(saveBundle);
        }

        [HttpGet]
        public IQueryable<Student> Students()
        {
            return PersistenceManager.Context.Students.Include("Enrollments");
        }
    }

    internal class SchoolPersistenceManager : EFPersistenceManager<SchoolContext>
    {
        public SchoolPersistenceManager(SchoolContext context) : base(context) { }

        // more infor can be found here
        // https://github.com/Breeze/breeze.server.net/blob/master/Tests/Test.AspNetCore.EFCore/Controllers/NorthwindIBModelController.cs    }
    }
}
