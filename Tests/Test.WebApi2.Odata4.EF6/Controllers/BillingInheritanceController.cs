using System.Linq;
using System.Web.Http;

using Inheritance.Models;
using System.Web.OData;

namespace Test.WebApi2.OData4.Controllers {

  public abstract class BaseBillingController<T> : ODataController where T : class {
    internal readonly InheritanceContext _context = new InheritanceContext();

    [EnableQuery]
    // [ODataRoute]
    public virtual IQueryable<T> Get() {
      return _context.Set<T>();
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