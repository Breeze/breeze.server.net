
using Breeze.AspNetCore;
using Breeze.Persistence;
using Breeze.Persistence.EFCore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ProduceTPH;
using System;
using System.Linq;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class ProduceTPHController : Controller {
    private ProducePersistenceManager PersistenceManager;

    // called via DI 
    public ProduceTPHController(ProduceTPHContext context) {
      PersistenceManager = new ProducePersistenceManager(context);
    }

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

    [HttpGet]
    public IQueryable<Fruit> Fruits() {
      return PersistenceManager.Context.ItemsOfProduce.OfType<Fruit>();
    }

    [HttpGet]
    public IQueryable<Apple> Apples() {
      return PersistenceManager.Context.ItemsOfProduce.OfType<Apple>();
    }
  }

  public class ProducePersistenceManager : EFPersistenceManager<ProduceTPHContext> {
    public ProducePersistenceManager(ProduceTPHContext dbContext) : base(dbContext) { }

    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      throw new NotImplementedException();
    }
  }
}
