using System.Linq;
using System.Web.Http;

using Inheritance.Models;
using System.Web.Http.OData;
using System.Threading.Tasks;
using System.Net;

namespace Test.WebApi2.OData.Controllers {

  public abstract class BaseBillingController<T> : ODataController where T : class {
    internal readonly InheritanceContext _context = new InheritanceContext();

    [EnableQuery]
    public virtual IQueryable<T> Get() {
      return _context.Set<T>();
    }

    // DELETE odata/TodoItems(5)
    public async Task<IHttpActionResult> Delete([FromODataUri] int key) {
      var items = _context.Set<T>();
      T item = await items.FindAsync(key);
      if (item == null) {
        return NotFound();
      }

      items.Remove(item);
      await _context.SaveChangesAsync();

      return StatusCode(HttpStatusCode.NoContent);
    }

  }

  public class AccountTypesController : BaseBillingController<AccountType> {

  }

  #region TPH

  public class BillingDetailTPHsController : BaseBillingController<BillingDetailTPH> {

  }

  public class BankAccountTPHsController : BaseBillingController<BankAccountTPH> {

    [EnableQuery]
    public override IQueryable<BankAccountTPH> Get() {
      return _context.Set<BillingDetailTPH>().OfType<BankAccountTPH>();
    }

  }

  public class CreditCardTPHsController : BaseBillingController<CreditCardTPH> {
    [EnableQuery]
    public override IQueryable<CreditCardTPH> Get() {
      return _context.Set<BillingDetailTPH>().OfType<CreditCardTPH>();
    }
  }

  #endregion

  #region TPT

  public class BillingDetailTPTsController : BaseBillingController<BillingDetailTPT> {

  }

  public class BankAccountTPTsController : BaseBillingController<BankAccountTPT> {
    [EnableQuery]
    public override IQueryable<BankAccountTPT> Get() {
      return _context.Set<BillingDetailTPT>().OfType<BankAccountTPT>();
    }
  }

  public class CreditCardTPTsController : BaseBillingController<CreditCardTPT> {
    [EnableQuery]
    public override IQueryable<CreditCardTPT> Get() {
      return _context.Set<BillingDetailTPT>().OfType<CreditCardTPT>();
    }
  }

  #endregion

  #region TPC

  public class BillingDetailTPCsController : BaseBillingController<BillingDetailTPC> {

  }

  public class BankAccountTPCsController : BaseBillingController<BankAccountTPC> {

  }

  public class CreditCardTPCsController : BaseBillingController<CreditCardTPC> {

  }


  #endregion

 
  #region Purge/Reset

  public class InheritanceController : ApiController {
    internal readonly InheritanceContext _context = new InheritanceContext();
    // ~/breeze/inheritance//purge
    [HttpPost]
    public string Purge() {
      InheritanceDbInitializer.PurgeDatabase(_context);
      return "purged";
    }

    // ~/breeze/inheritance//reset
    [HttpPost]
    public string Reset() {
      Purge();
      InheritanceDbInitializer.ResetDatabase(_context);
      return "reset";
    }

  }

  #endregion
  
}