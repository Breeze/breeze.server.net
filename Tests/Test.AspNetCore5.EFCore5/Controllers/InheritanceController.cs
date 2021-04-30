// Either EFCORE or NHIBERNATE should be defined in the project properties

using Breeze.AspNetCore;
using Breeze.Persistence;
#if EFCORE
using Breeze.Persistence.EFCore;
#endif
using Inheritance.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class InheritanceController : Controller {
#if EFCORE
    private EFPersistenceManager<InheritanceContext> PersistenceManager;
    public InheritanceController(InheritanceContext context) {
      PersistenceManager = new EFPersistenceManager<InheritanceContext>(context);
    }
#elif NHIBERNATE
    private InheritancePersistenceManager PersistenceManager;
    public InheritanceController(NHSessionProvider<InheritancePersistenceManager> provider) {
      PersistenceManager = new InheritancePersistenceManager(provider);
    }
#endif
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
#if EFCORE
      return PersistenceManager.Context.BankAccountTPCs.OfType<BankAccountTPC>();
#elif NHIBERNATE
      return PersistenceManager.Context.BankAccountTPCs;
#endif
    }

    // ~/breeze/inheritance/creditCardsTPC
    [HttpGet]
    public IQueryable<CreditCardTPC> CreditCardTPCs() {
      //return PersistenceManager.Context.BillingDetailTPCs.OfType<CreditCardTPC>();
#if EFCORE
      return PersistenceManager.Context.CreditCardTPCs.OfType<CreditCardTPC>();
#elif NHIBERNATE
      return PersistenceManager.Context.CreditCardTPCs;
#endif
    }
    #endregion

    #region Purge/Reset
    // ~/breeze/inheritance//purge
    [HttpPost]
    public string Purge() {
#if EFCORE
      InheritanceDbInitializer.PurgeDatabase(PersistenceManager.Context);
#elif NHIBERNATE
      PersistenceManager.PurgeDatabase();
#endif
      return "purged";
    }

    // ~/breeze/inheritance//reset
    [HttpPost]
    public string Reset() {
      Purge();
#if EFCORE
      InheritanceDbInitializer.ResetDatabase(PersistenceManager.Context);
#elif NHIBERNATE
      PersistenceManager.ResetDatabase();
#endif
      return "reset";
    }

    [HttpPost]
    public string Seed() {
#if EFCORE
      InheritanceDbInitializer.Seed(PersistenceManager.Context);
#elif NHIBERNATE
      PersistenceManager.Seed();
#endif
      return "seed";
    }
#endregion

    }
#if NHIBERNATE
  public class InheritancePersistenceManager : ValidatingPersistenceManager {
    public InheritancePersistenceManager(NHSessionProvider<InheritancePersistenceManager> provider) : base(provider.OpenSession()) { }
    public InheritancePersistenceManager Context {
      get { return this; }
    }

    public IQueryable<AccountType> AccountTypes {
      get { return GetQuery<AccountType>(); }
    }

    public IQueryable<BillingDetailTPH> BillingDetailTPHs {
      get { return GetQuery<BillingDetailTPH>(); }
    }
    public IQueryable<BankAccountTPH> BankAccountTPHs {
      get { return GetQuery<BankAccountTPH>(); }
    }
    public IQueryable<CreditCardTPH> CreditCardTPHs {
      get { return GetQuery<CreditCardTPH>(); }
    }

    public IQueryable<BillingDetailTPT> BillingDetailTPTs {
      get { return GetQuery<BillingDetailTPT>(); }
    }
    public IQueryable<BankAccountTPC> BankAccountTPCs {
      get { return GetQuery<BankAccountTPC>(); }
    }
    public IQueryable<CreditCardTPC> CreditCardTPCs {
      get { return GetQuery<CreditCardTPC>(); }
    }

    public void Seed() {
      PurgeDatabase();
      ResetDatabase();
    }

    public void PurgeDatabase() {
      var session = this.Session;
      session.Flush();

      session.CreateSQLQuery("delete from DepositTPCs").ExecuteUpdate();
      session.CreateSQLQuery("delete from DepositTPHs").ExecuteUpdate();
      session.CreateSQLQuery("delete from DepositTPTs").ExecuteUpdate();

      session.CreateSQLQuery("delete from BankAccountTPCs").ExecuteUpdate();
      session.CreateSQLQuery("delete from BankAccountTPTs").ExecuteUpdate();

      session.CreateSQLQuery("delete from CreditCardsTPCs").ExecuteUpdate();
      session.CreateSQLQuery("delete from CreditCardTPTs").ExecuteUpdate();

      session.CreateSQLQuery("delete from BillingDetailTPHs").ExecuteUpdate();
      session.CreateSQLQuery("delete from BillingDetailTPTs").ExecuteUpdate();
      session.Flush();
    }

    public void ResetDatabase() {
      var session = this.Session;
      IBillingDetail[] billingDetails;

      billingDetails = InheritanceDataMaker.MakeData<BillingDetailTPH, BankAccountTPH, CreditCardTPH>("TPH");
      Array.ForEach((BillingDetailTPH[])billingDetails, _ => {
        session.Save(_);
        var deps = InheritanceDataMaker.MakeDeposits<BillingDetailTPH, DepositTPH>(_);
        Array.ForEach(deps, d => session.Save(d));
      });
      session.Flush();

      billingDetails = InheritanceDataMaker.MakeData<BillingDetailTPT, BankAccountTPT, CreditCardTPT>("TPT");
      Array.ForEach((BillingDetailTPT[])billingDetails, _ => {
        session.Save(_);
        var deps = InheritanceDataMaker.MakeDeposits<BillingDetailTPT, DepositTPT>(_);
        Array.ForEach(deps, d => session.Save(d));
      });
      session.Flush();

      billingDetails = InheritanceDataMaker.MakeData<BillingDetailTPC, BankAccountTPC, CreditCardTPC>("TPC");
      Array.ForEach((BillingDetailTPC[])billingDetails, _ => {
        session.Save(_);
        var deps = InheritanceDataMaker.MakeDeposits<BillingDetailTPC, DepositTPC>(_);
        Array.ForEach(deps, d => session.Save(d));
      });

      session.Flush();

    }
  }
#endif
}
