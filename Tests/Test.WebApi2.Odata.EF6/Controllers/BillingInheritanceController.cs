using System.Linq;
using System.Web.Http;

using Inheritance.Models;
using System.Web.Http.OData;

namespace Test.WebApi2.OData.Controllers {

  public abstract class BaseBillingController<T> : ODataController where T : class {
    internal readonly InheritanceContext _context = new InheritanceContext();

    [Queryable]
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

    [Queryable]
    public override IQueryable<BankAccountTPH> Get() {
      return _context.Set<BillingDetailTPH>().OfType<BankAccountTPH>();
    }

  }

  public class CreditCardTPHsController : BaseBillingController<CreditCardTPH> {
    [Queryable]
    public override IQueryable<CreditCardTPH> Get() {
      return _context.Set<BillingDetailTPH>().OfType<CreditCardTPH>();
    }
  }

  #endregion

  #region TPT

  public class BillingDetailTPTsController : BaseBillingController<BillingDetailTPT> {

  }

  public class BankAccountTPTsController : BaseBillingController<BankAccountTPT> {
    [Queryable]
    public override IQueryable<BankAccountTPT> Get() {
      return _context.Set<BillingDetailTPT>().OfType<BankAccountTPT>();
    }
  }

  public class CreditCardTPTsController : BaseBillingController<CreditCardTPT> {
    [Queryable]
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