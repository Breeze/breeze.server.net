// Either EFCORE or NHIBERNATE should be defined in the project properties

using Breeze.AspNetCore;
using Breeze.Persistence;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
#if EFCORE
using Breeze.Persistence.EFCore;
using ProduceTPH;
#elif NHIBERNATE
using Models.Produce.NH;
#endif
using System;
using System.Linq;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class ProduceTPHController : Controller {
#if EFCORE
    private ProducePersistenceManager PersistenceManager;
    public ProduceTPHController(ProduceTPHContext context) {
      PersistenceManager = new ProducePersistenceManager(context);
    }
#elif NHIBERNATE
    private ProducePersistenceManager PersistenceManager;
    public ProduceTPHController(NHSessionProvider<ProducePersistenceManager> provider) {
      PersistenceManager = new ProducePersistenceManager(provider);
    }
#endif

    [HttpGet]
    public String Metadata() {
      return PersistenceManager.Metadata();
    }

    [HttpPost]
    public SaveResult SaveChanges(JObject saveBundle) {
      return PersistenceManager.SaveChanges(saveBundle);
    }

    [HttpGet]
    public IQueryable<ItemOfProduce> ItemsOfProduce() {
      return PersistenceManager.Context.ItemsOfProduce;
    }
#if NHIBERNATE
    [HttpGet]
    public IQueryable<ItemOfProduce> ItemOfProduces() {
      return PersistenceManager.Context.ItemsOfProduce;
    }
#endif

    [HttpGet]
    public IQueryable<Fruit> Fruits() {
      return PersistenceManager.Context.ItemsOfProduce.OfType<Fruit>();
    }

    [HttpGet]
    public IQueryable<Apple> Apples() {
      return PersistenceManager.Context.ItemsOfProduce.OfType<Apple>();
    }
  }

#if EFCORE
  public class ProducePersistenceManager : EFPersistenceManager<ProduceTPHContext> {
    public ProducePersistenceManager(ProduceTPHContext dbContext) : base(dbContext) { }

    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      throw new NotImplementedException();
    }
  }
#elif NHIBERNATE
  public class ProducePersistenceManager : ValidatingPersistenceManager {
    public ProducePersistenceManager(NHSessionProvider<ProducePersistenceManager> provider) : base(provider.OpenSession()) { }

    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      throw new NotImplementedException();
    }

    public ProducePersistenceManager Context {
      get { return this; }
    }

    public IQueryable<ItemOfProduce> ItemsOfProduce {
      get { return GetQuery<ItemOfProduce>(); }
    }

  }
#endif

}
