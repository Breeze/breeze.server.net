using System.Linq;
using System.Web.Http;

using Inheritance.Models;
using System.Web.OData;

using ProduceTPH;

namespace Test.WebApi2.OData4.Controllers {

  public abstract class BaseProduceController<T> : ODataController where T : class {
    internal readonly ProduceTPHContext _context = new ProduceTPHContext();

    [EnableQuery]
    public virtual IQueryable<T> Get() {
      return _context.Set<T>();
    }
  }

  public class ItemsOfProduceController : BaseProduceController<ItemOfProduce> {

  }

  public class FruitsController : BaseProduceController<Fruit> {
    
  }
 

 
  #region Purge/Reset

  //public class XXX {
  //  // ~/breeze/inheritance//purge
  //  [HttpPost]
  //  public string Purge() {
  //    InheritanceDbInitializer.PurgeDatabase(_contextProvider.Context);
  //    return "purged";
  //  }

  //  // ~/breeze/inheritance//reset
  //  [HttpPost]
  //  public string Reset() {
  //    Purge();
  //    InheritanceDbInitializer.ResetDatabase(_contextProvider.Context);
  //    return "reset";
  //  }

  //}

  #endregion
  
}