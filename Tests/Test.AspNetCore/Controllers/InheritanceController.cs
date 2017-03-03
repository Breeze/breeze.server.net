using System.Linq;


using Breeze.Persistence;
using Breeze.Persistence.EF6;


using Newtonsoft.Json.Linq;

using Inheritance.Models;
using Microsoft.AspNetCore.Mvc;
using Breeze.AspNetCore;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class InheritanceController : Controller {

    readonly EFPersistenceManager<InheritanceContext> _persistenceManager =
        new EFPersistenceManager<InheritanceContext>();

    // ~/breeze/inheritance/Metadata 
    [HttpGet]
    public string Metadata() {
      return _persistenceManager.Metadata();
    }

    // ~/breeze/inheritance/SaveChanges
    [HttpPost]
    public SaveResult SaveChanges([FromBody] JObject saveBundle) {
      return _persistenceManager.SaveChanges(saveBundle);
    }

    // ~/breeze/inheritance/accountTypes
    [HttpGet]
    public IQueryable<AccountType> AccountTypes() {
      return _persistenceManager.Context.AccountTypes;
    }

    #region TPH

    // ~/breeze/inheritance/billingDetailsTPH
    [HttpGet]
    public IQueryable<BillingDetailTPH> BillingDetailTPHs() {
      return _persistenceManager.Context.BillingDetailTPHs;
    }

    // ~/breeze/inheritance/bankAccountTPH
    [HttpGet]
    public IQueryable<BankAccountTPH> BankAccountTPHs() {
      return _persistenceManager.Context.BillingDetailTPHs.OfType<BankAccountTPH>();
    }

    // ~/breeze/inheritance/creditCardsTPH
    [HttpGet]
    public IQueryable<CreditCardTPH> CreditCardTPHs() {
      return _persistenceManager.Context.BillingDetailTPHs.OfType<CreditCardTPH>();
    }
    #endregion

    #region TPT

    // ~/breeze/inheritance/billingDetailsTPT
    [HttpGet]
    public IQueryable<BillingDetailTPT> BillingDetailTPTs() {
      return _persistenceManager.Context.BillingDetailTPTs;
    }

    // ~/breeze/inheritance/bankAccountTPT
    [HttpGet]
    public IQueryable<BankAccountTPT> BankAccountTPTs() {
      return _persistenceManager.Context.BillingDetailTPTs.OfType<BankAccountTPT>();
    }

    // ~/breeze/inheritance/creditCardsTPT
    [HttpGet]
    public IQueryable<CreditCardTPT> CreditCardTPTs() {
      return _persistenceManager.Context.BillingDetailTPTs.OfType<CreditCardTPT>();
    }
    #endregion

    #region TPC

    // ~/breeze/inheritance/billingDetailsTPC
    [HttpGet]
    public IQueryable<BillingDetailTPC> BillingDetailTPCs() {
      return _persistenceManager.Context.BillingDetailTPCs;
    }

    // ~/breeze/inheritance/bankAccountTPC
    [HttpGet]
    public IQueryable<BankAccountTPC> BankAccountTPCs() {
      return _persistenceManager.Context.BillingDetailTPCs.OfType<BankAccountTPC>();
    }

    // ~/breeze/inheritance/creditCardsTPC
    [HttpGet]
    public IQueryable<CreditCardTPC> CreditCardTPCs() {
      return _persistenceManager.Context.BillingDetailTPCs.OfType<CreditCardTPC>();
    }
    #endregion

    #region Purge/Reset

    // ~/breeze/inheritance//purge
    [HttpPost]
    public string Purge() {
      InheritanceDbInitializer.PurgeDatabase(_persistenceManager.Context);
      return "purged";
    }

    // ~/breeze/inheritance//reset
    [HttpPost]
    public string Reset() {
      Purge();
      InheritanceDbInitializer.ResetDatabase(_persistenceManager.Context);
      return "reset";
    }

    #endregion
  }
}