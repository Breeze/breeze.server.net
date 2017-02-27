// #define NHIBERNATE

using System;
using System.Collections.Generic;
using System.Linq;

using Breeze.ContextProvider;

using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;

#if NHIBERNATE
using Breeze.ContextProvider.NH;
using Models.Produce.NH;
using NHibernate;
using NHibernate.Linq;
#else
using Breeze.ContextProvider.EF6;
using ProduceTPH;
#endif


namespace Breeze.AspCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [QueryFilter]
  public class ProduceTPHController : Controller {

    private ProduceTPHContextProvider ContextProvider;

    public ProduceTPHController(ProduceTPHContext context) {
      ContextProvider = new ProduceTPHContextProvider(context);
    }



    [HttpGet]
    public String Metadata() {
#if NHIBERNATE
      return ContextProvider.GetHardcodedMetadata();
#else
      return ContextProvider.Metadata();
#endif
    }

    [HttpPost]
    public SaveResult SaveChanges(JObject saveBundle) {
      return ContextProvider.SaveChanges(saveBundle);
    }

    #region standard queries

    [HttpGet]
    public IQueryable<ItemOfProduce> ItemsOfProduce() {
      return ContextProvider.Context.ItemsOfProduce;
    }


    [HttpGet]
    public IQueryable<Fruit> Fruits() {
      return ContextProvider.Context.ItemsOfProduce.OfType<Fruit>();
    }


    [HttpGet]
    public IQueryable<Apple> Apples() {
      return ContextProvider.Context.ItemsOfProduce.OfType<Apple>();
    }

    #endregion

    #region named queries

    #endregion
  }



#if NHIBERNATE
  public class ProduceTPHContextProvider  : ProduceNHContext {
#else
  public class ProduceTPHContextProvider  : EFContextProvider<ProduceTPHContext> {

    public ProduceTPHContextProvider(ProduceTPHContext dbContext) : base(dbContext) { }
#endif



    protected override bool BeforeSaveEntity(EntityInfo entityInfo) {
        return true;
    }

    protected override Dictionary<Type, List<EntityInfo>> BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap) {
      return saveMap;
    }


    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      throw new NotImplementedException();
    }
  }

  

}