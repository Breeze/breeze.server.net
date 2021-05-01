using Breeze.AspNetCore;
using Breeze.Persistence;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

using Sample_WebApi2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Test.AspNetCore.Controllers {

  [Route("breeze/[controller]/[action]")]
  [BreezeQueryFilter]
  public class NonEFModelController : Controller {


    NonEFPersistenceManager PersistenceManager = new NonEFPersistenceManager();


    [HttpGet]
    public String Metadata() {
      return PersistenceManager.Metadata();
    }

    [HttpPost]
    public SaveResult SaveChanges(JObject saveBundle) {
      return PersistenceManager.SaveChanges(saveBundle);
    }

    #region standard queries

    [HttpGet]
    public IQueryable<Person> Persons() {
      var custs = PersistenceManager.Context.Persons;
      return custs;
    }

    [HttpGet]
    public IQueryable<Meal> Meals() {
      var orders = PersistenceManager.Context.Meals;
      return orders;
    }


    #endregion

    #region named queries

    #endregion
  }



  public class NonEFPersistenceManager : Breeze.Persistence.PersistenceManager {

    public NonEFModelContext Context = new NonEFModelContext();


    protected override bool BeforeSaveEntity(EntityInfo entityInfo) {
      return true;
    }

    protected override Dictionary<Type, List<EntityInfo>> BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap) {
      return saveMap;
    }

    public override IDbConnection GetDbConnection() { return null; }

    protected override void OpenDbConnection() { }

    protected override void CloseDbConnection() { }


    protected override string BuildJsonMetadata() {
      return null;
    }

    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      throw new NotImplementedException();
    }

    protected override Task OpenDbConnectionAsync(CancellationToken cancellationToken) {
      throw new NotImplementedException();
    }

    protected override Task CloseDbConnectionAsync() {
      throw new NotImplementedException();
    }

    protected override Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) {
      throw new NotImplementedException();
    }

    protected override Task SaveChangesCoreAsync(SaveWorkState saveWorkState, CancellationToken cancellationToken) {
      throw new NotImplementedException();
    }
  }


}
