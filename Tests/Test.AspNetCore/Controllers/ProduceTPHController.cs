// #define NHIBERNATE

using System;
using System.Collections.Generic;
using System.Linq;

using Breeze.Persistence;

using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Breeze.AspNetCore;

#if NHIBERNATE
using Breeze.Persistence.NH;
using Models.Produce.NH;
using NHibernate;
using NHibernate.Linq;
#else
using Breeze.Persistence.EF6;
using ProduceTPH;
#endif


namespace Test.AspNetCore.Controllers {

    [Route("breeze/[controller]/[action]")]
    [BreezeQueryFilter]
    public class ProduceTPHController : Controller {

        private ProduceTPHPersistenceManager PersistenceManager;

        public ProduceTPHController(ProduceTPHContext context) {
            PersistenceManager = new ProduceTPHPersistenceManager(context);
        }



        [HttpGet]
        public String Metadata() {
#if NHIBERNATE
      return PersistenceManager.GetHardcodedMetadata();
#else
            return PersistenceManager.Metadata();
#endif
        }

        [HttpPost]
        public SaveResult SaveChanges(JObject saveBundle) {
            return PersistenceManager.SaveChanges(saveBundle);
        }

        #region standard queries

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

        #endregion

        #region named queries

        #endregion
    }



#if NHIBERNATE
  public class ProduceTPHPersistenceManager  : ProduceNHContext {
#else
    public class ProduceTPHPersistenceManager : EFPersistenceManager<ProduceTPHContext> {

        public ProduceTPHPersistenceManager(ProduceTPHContext dbContext) : base(dbContext) { }
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