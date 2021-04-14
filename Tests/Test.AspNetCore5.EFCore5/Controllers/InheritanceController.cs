using Breeze.AspNetCore;
using Breeze.Persistence;
using Breeze.Persistence.EFCore;
using Inheritance.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class InheritanceController : Controller {
    private EFPersistenceManager<InheritanceContext> PersistenceManager;

    // called via DI 
    public InheritanceController(InheritanceContext context) {
      PersistenceManager = new EFPersistenceManager<InheritanceContext>(context);
    }

    [HttpGet]
    public string Metadata() {
      return PersistenceManager.Metadata();
    }

    [HttpPost]
    public SaveResult SaveChanges([FromBody] JObject saveBundle) {
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpGet]
    public IQueryable<AccountType> AccountTypes() {
      return PersistenceManager.Context.AccountTypes;
    }


    #region TPH

    // ~/breeze/inheritance/billingDetailsTPH
    [HttpGet]
    public IQueryable<BillingDetailTPH> BillingDetailTPHs() {
      return PersistenceManager.Context.BillingDetailTPHs;
    }

    // ~/breeze/inheritance/bankAccountTPH
    [HttpGet]
    public IQueryable<BankAccountTPH> BankAccountTPHs() {
      return PersistenceManager.Context.BillingDetailTPHs.OfType<BankAccountTPH>();
    }

    // ~/breeze/inheritance/creditCardsTPH
    [HttpGet]
    public IQueryable<CreditCardTPH> CreditCardTPHs() {
      return PersistenceManager.Context.BillingDetailTPHs.OfType<CreditCardTPH>();
    }
    #endregion

    #region TPT

    // ~/breeze/inheritance/billingDetailsTPT
    [HttpGet]
    public IQueryable<BillingDetailTPT> BillingDetailTPTs() {
      return PersistenceManager.Context.BillingDetailTPTs;
    }

    // ~/breeze/inheritance/bankAccountTPT
    [HttpGet]
    public IQueryable<BankAccountTPT> BankAccountTPTs() {
      return PersistenceManager.Context.BillingDetailTPTs.OfType<BankAccountTPT>();
    }

    // ~/breeze/inheritance/creditCardsTPT
    [HttpGet]
    public IQueryable<CreditCardTPT> CreditCardTPTs() {
      return PersistenceManager.Context.BillingDetailTPTs.OfType<CreditCardTPT>();
    }
    #endregion

    #region TPC

    // ~/breeze/inheritance/billingDetailsTPC
    [HttpGet]
    public IQueryable<BillingDetailTPC> BillingDetailTPCs() {
      //return PersistenceManager.Context.BillingDetailTPCs;
      var ba = PersistenceManager.Context.BankAccountTPCs.ToList<BillingDetailTPC>();
      var cc = PersistenceManager.Context.CreditCardTPCs.ToList<BillingDetailTPC>();
      return ba.Concat(cc).AsQueryable();
    }

    // ~/breeze/inheritance/bankAccountTPC
    [HttpGet]
    public IQueryable<BankAccountTPC> BankAccountTPCs() {
      //return PersistenceManager.Context.BillingDetailTPCs.OfType<BankAccountTPC>();
      return PersistenceManager.Context.BankAccountTPCs.OfType<BankAccountTPC>();
    }

    // ~/breeze/inheritance/creditCardsTPC
    [HttpGet]
    public IQueryable<CreditCardTPC> CreditCardTPCs() {
      //return PersistenceManager.Context.BillingDetailTPCs.OfType<CreditCardTPC>();
      return PersistenceManager.Context.CreditCardTPCs.OfType<CreditCardTPC>();
    }
    #endregion

    #region Purge/Reset

    // ~/breeze/inheritance//purge
    [HttpPost]
    public string Purge() {
      InheritanceDbInitializer.PurgeDatabase(PersistenceManager.Context);
      return "purged";
    }

    // ~/breeze/inheritance//reset
    [HttpPost]
    public string Reset() {
      Purge();
      InheritanceDbInitializer.ResetDatabase(PersistenceManager.Context);
      return "reset";
    }

    [HttpPost]
    public string Seed() {
      InheritanceDbInitializer.Seed(PersistenceManager.Context);
      return "seed";
    }

    #endregion

  }
}
