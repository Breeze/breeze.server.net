using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate;
using NHibernate.Metadata;
using NHibernate.Type;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Breeze.Core;
using Newtonsoft.Json.Converters;

namespace Breeze.Persistence.NH {
  public class NHPersistenceManager : Breeze.Persistence.PersistenceManager, IDisposable {
    private readonly ISession session;
    //protected Configuration configuration;
    private static readonly Dictionary<ISessionFactory, NHBreezeMetadata> _factoryMetadata = new Dictionary<ISessionFactory, NHBreezeMetadata>();
    private static readonly object _metadataLock = new object();

    static NHPersistenceManager() {
      EntityQuery.NeedsExecution = NHQueryHelper.NeedsExecution;
      EntityQuery.ApplyCustomLogic = (eq, iq, type) => iq.Provider.CreateQuery(iq.Expression);
      EntityQuery.AfterExecution = NHQueryHelper.PostExecuteQuery;
    }

    /// <summary>
    /// Create a new context for the given session.  
    /// Each thread should have its own NHContext and Session.
    /// </summary>
    /// <param name="session">Used for queries and updates</param>
    public NHPersistenceManager(ISession session) {
      this.session = session;
    }

    /// <summary>
    /// Creates a new context using the session and metadata from the sourceContext
    /// </summary>
    /// <param name="sourceContext">source of the Session and metadata used by this new context.</param>
    public NHPersistenceManager(NHPersistenceManager sourceContext) {
      this.session = sourceContext.Session;
      this._metadata = sourceContext.GetMetadata();
    }

    public ISession Session {
      get { return session; }
    }

    /// <summary>
    /// Return a query for the given entity
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="cacheable">Whether to mark the query Cacheable.  Default is false.</param>
    /// <returns></returns>
    public IQueryable<T> GetQuery<T>(bool cacheable = false) {
      var q = session.Query<T>();
      if (cacheable) {
        q = q.WithOptions(o => o.SetCacheable(true));
      }
      return q;
    }

    /// <summary>
    /// Return a cacheable query for the given entity, using the given cache region
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="cacheRegion">Cache region to use.</param>
    /// <returns></returns>
    public IQueryable<T> GetQuery<T>(string cacheRegion) {
      var q = session.Query<T>().WithOptions(o => {
        o.SetCacheable(true).SetCacheRegion(cacheRegion);
      });
      return q;
    }

    /// <summary>
    /// Close the session
    /// </summary>
    public void Close() {
      if (session != null && session.IsOpen) session.Close();
    }

    /// <summary>
    /// Close the session
    /// </summary>
    public void Dispose() {
      Close();
    }

    /// <returns>The connection from the session.</returns>
    public override IDbConnection GetDbConnection() {
      return session.Connection;
    }

    protected override void OpenDbConnection() {
      // already open when session is created
    }

    /// <summary>
    /// Close the session and its associated db connection
    /// </summary>
    protected override void CloseDbConnection() {
      if (session != null && session.IsOpen) {
        var dbc = session.Close();
        if (dbc != null) dbc.Close();
      }
    }

    protected override IDbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel) {
      var itran = session.BeginTransaction(isolationLevel);
      var wrapper = new NHTransactionWrapper(itran, session.Connection, isolationLevel);
      return wrapper;
    }

    public override object[] GetKeyValues(EntityInfo entityInfo) {
      return GetKeyValues(entityInfo.Entity);
    }

    public object[] GetKeyValues(object entity) {
      var classMeta = session.SessionFactory.GetClassMetadata(entity.GetType());
      if (classMeta == null) {
        throw new ArgumentException("Metadata not found for type " + entity.GetType());
      }
      var keyValues = GetIdentifierAsArray(entity, classMeta);
      return keyValues;
    }

    /// <summary>
    /// Allows subclasses to process entities before they are saved.  This method is called
    /// after BeforeSaveEntities(saveMap), and before any session.Save methods are called.
    /// The foreign-key associations on the entities have been resolved, relating the entities
    /// to each other, and attaching proxies for other many-to-one associations.
    /// </summary>
    /// <param name="entitiesToPersist">List of entities in the order they will be saved</param>
    /// <returns>The same entitiesToPersist.  Overrides of this method may modify the list.</returns>
    public virtual List<EntityInfo> BeforeSaveEntityGraph(List<EntityInfo> entitiesToPersist) {
      return entitiesToPersist;
    }

    /// <summary>
    /// If TypeFilter function is defined, returns TypeFilter(entityInfo.Entity.GetType())
    /// </summary>
    /// <param name="entityInfo"></param>
    /// <returns>true if the entity should be saved, false if not</returns>
    protected override bool BeforeSaveEntity(EntityInfo entityInfo) {
      if (!base.BeforeSaveEntity(entityInfo)) return false;
      if (this.TypeFilter == null) return true;
      return this.TypeFilter(entityInfo.Entity.GetType());
    }

    #region Metadata

    /// <summary>
    /// Sets a function to filter types from metadata generation and SaveChanges.
    /// The function returns true if a Type should be included, false otherwise.
    /// </summary><example>
    /// // exclude the LogRecord entity
    /// MyNHContext.TypeFilter = (type) => type.Name != "LogRecord";
    /// </example><example>
    /// // exclude certain entities, and all Audit* entities
    /// var excluded = new string[] { "Comment", "LogRecord", "UserPermission" };
    /// MyNHContext.TypeFilter = (type) =>
    /// {
    ///   if (excluded.Contains(type.Name)) return false;
    ///   if (type.Name.StartsWith("Audit")) return false;
    ///   return true;
    /// };
    /// </example>
    public Func<Type, bool> TypeFilter { get; set; }

    protected override string BuildJsonMetadata() {
      var meta = GetMetadata();
      var jss = new JsonSerializerSettings() {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
      };
      jss.Converters.Add(new StringEnumConverter());

      var json = JsonConvert.SerializeObject(meta, Formatting.Indented, jss);
      return json;
    }

    protected NHBreezeMetadata GetMetadata() {
      if (_metadata == null) {
        lock (_metadataLock) {
          if (!_factoryMetadata.TryGetValue(session.SessionFactory, out _metadata)) {
            var builder = new NHMetadataBuilder(session.SessionFactory);
            _metadata = builder.BuildMetadata(TypeFilter);
            _factoryMetadata.Add(session.SessionFactory, _metadata);
          }
        }
      }
      return _metadata;
    }

    #endregion
    #region Save Changes

    private readonly Dictionary<EntityInfo, KeyMapping> EntityKeyMapping = new Dictionary<EntityInfo, KeyMapping>();
    private readonly List<EntityError> entityErrors = new List<EntityError>();
    private NHBreezeMetadata _metadata;

    /// <summary>
    /// Persist the changes to the entities in the saveMap.
    /// This implements the abstract method in PersistenceManager.
    /// Assigns saveWorkState.KeyMappings, which map the temporary keys to their real generated keys.
    /// Note that this method sets session.FlushMode = FlushMode.Never, so manual flushes are required.
    /// </summary>
    /// <param name="saveMap">Map of Type -> List of entities of that type</param>
    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      var saveMap = saveWorkState.SaveMap;
      session.FlushMode = FlushMode.Manual;
      var tx = session.Transaction;
      var hasExistingTransaction = tx.IsActive;
      if (!hasExistingTransaction) tx.Begin(BreezeConfig.Instance.GetTransactionSettings().IsolationLevelAs);
      try {
        // Relate entities in the saveMap to other NH entities, so NH can save the FK values.
        var fixer = GetRelationshipFixer(saveMap);
        var saveOrder = fixer.FixupRelationships();

        // Allow subclass to process entities before we save them
        saveOrder = BeforeSaveEntityGraph(saveOrder);

        ProcessSaves(saveOrder);

        session.Flush();
        RefreshFromSession(saveMap);
        if (!hasExistingTransaction) tx.Commit();
        fixer.RemoveRelationships();
      } catch (PropertyValueException pve) {
        // NHibernate can throw this
        if (!hasExistingTransaction) tx.Rollback();
        entityErrors.Add(new EntityError() {
          EntityTypeName = pve.EntityName,
          ErrorMessage = pve.Message,
          ErrorName = "PropertyValueException",
          KeyValues = null,
          PropertyName = pve.PropertyName
        });
        saveWorkState.EntityErrors = entityErrors;
      } catch (Exception) {
        if (!hasExistingTransaction) tx.Rollback();
        throw;
      } finally {
        if (!hasExistingTransaction) tx.Dispose();
      }

      saveWorkState.KeyMappings = UpdateAutoGeneratedKeys(saveWorkState.EntitiesWithAutoGeneratedKeys);
    }

    /// <summary>
    /// Get a new NHRelationshipFixer using the saveMap and the foreign-key map from the metadata.
    /// </summary>
    /// <param name="saveMap"></param>
    /// <returns></returns>
    protected NHRelationshipFixer GetRelationshipFixer(Dictionary<Type, List<EntityInfo>> saveMap) {
      // Get the map of foreign key relationships from the metadata
      var fkMap = GetMetadata().ForeignKeyMap;
      return new NHRelationshipFixer(saveMap, fkMap, session);
    }

    /// <summary>
    /// Persist the changes to the entities in the saveOrder.
    /// </summary>
    /// <param name="saveOrder"></param>
    protected void ProcessSaves(List<EntityInfo> saveOrder) {

      var sessionFactory = session.SessionFactory;
      foreach (var entityInfo in saveOrder) {
        var entityType = entityInfo.Entity.GetType();
        var classMeta = sessionFactory.GetClassMetadata(entityType);
        AddKeyMapping(entityInfo, entityType, classMeta);
        ProcessEntity(entityInfo, classMeta);
      }
    }


    /// <summary>
    /// Add, update, or delete the entity according to its EntityState.
    /// </summary>
    /// <param name="entityInfo"></param>
    protected void ProcessEntity(EntityInfo entityInfo, IClassMetadata classMeta) {
      var entity = entityInfo.Entity;
      var state = entityInfo.EntityState;

      // Restore the old value of the concurrency column so Hibernate will be able to save the entity
      if (classMeta.IsVersioned) {
        RestoreOldVersionValue(entityInfo, classMeta);
      }

      if (state == EntityState.Modified) {
        CheckForKeyUpdate(entityInfo, classMeta);
        session.Update(entity);
      } else if (state == EntityState.Added) {
        session.Save(entity);
      } else if (state == EntityState.Deleted) {
        session.Delete(entity);
      } else {
        // Ignore EntityState.Unchanged.  Too many problems using session.Lock or session.Merge
        //session.Lock(entity, LockMode.None);
      }
    }

    protected void CheckForKeyUpdate(EntityInfo entityInfo, IClassMetadata classMeta) {
      if (classMeta.HasIdentifierProperty && entityInfo.OriginalValuesMap != null
        && entityInfo.OriginalValuesMap.ContainsKey(classMeta.IdentifierPropertyName)) {
        var errors = new EntityError[1] {
            new EntityError() {
              EntityTypeName = entityInfo.Entity.GetType().FullName,
              ErrorMessage = "Cannot update part of the entity's key",
              ErrorName = "KeyUpdateException",
              KeyValues = GetIdentifierAsArray(entityInfo.Entity, classMeta),
              PropertyName = classMeta.IdentifierPropertyName
            }};
        throw new EntityErrorsException("Cannot update part of the entity's key", errors);
      }
    }

    /// <summary>
    /// Restore the old value of the concurrency column so Hibernate will save the entity.
    /// Otherwise it will complain because Breeze has already changed the value.
    /// </summary>
    /// <param name="entityInfo"></param>
    /// <param name="classMeta"></param>
    protected void RestoreOldVersionValue(EntityInfo entityInfo, IClassMetadata classMeta) {
      if (entityInfo.OriginalValuesMap == null || entityInfo.OriginalValuesMap.Count == 0) return;
      var vcol = classMeta.VersionProperty;
      var vname = classMeta.PropertyNames[vcol];
      object oldVersion;
      if (entityInfo.OriginalValuesMap.TryGetValue(vname, out oldVersion)) {
        var entity = entityInfo.Entity;
        var vtype = classMeta.PropertyTypes[vcol].ReturnedClass;
        oldVersion = Convert.ChangeType(oldVersion, vtype);     // because JsonConvert makes all integers Int64
        classMeta.SetPropertyValue(entity, vname, oldVersion);
      }
    }

    /// <summary>
    /// Record the value of the temporary key in EntityKeyMapping
    /// </summary>
    /// <param name="entityInfo"></param>
    protected void AddKeyMapping(EntityInfo entityInfo, Type type, IClassMetadata meta) {
      if (entityInfo.EntityState != EntityState.Added) return;
      var entity = entityInfo.Entity;
      var id = GetIdentifier(entity, meta);
      var km = new KeyMapping() { EntityTypeName = type.FullName, TempValue = id };
      EntityKeyMapping.Add(entityInfo, km);
    }

    /// <summary>
    /// Get the identifier value for the entity.  If the entity does not have an
    /// identifier property, or natural identifiers defined, then the entity itself is returned.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected object GetIdentifier(object entity, IClassMetadata meta = null) {
      var type = entity.GetType();
      meta = meta ?? session.SessionFactory.GetClassMetadata(type);

      if (meta.IdentifierType != null) {
        var id = meta.GetIdentifier(entity);
        if (meta.IdentifierType.IsComponentType) {
          var compType = (ComponentType)meta.IdentifierType;
          return compType.GetPropertyValues(id);
        } else {
          return id;
        }
      } else if (meta.HasNaturalIdentifier) {
        var idprops = meta.NaturalIdentifierProperties;
        var values = meta.GetPropertyValues(entity);
        var idvalues = idprops.Select(i => values[i]).ToArray();
        return idvalues;
      }
      return entity;
    }

    /// <summary>
    /// Get the identier value for the entity as an object[].
    /// This is needed for creating an EntityError.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected object[] GetIdentifierAsArray(object entity, IClassMetadata meta) {
      var value = GetIdentifier(entity, meta);
      if (value.GetType().IsArray) {
        return (object[])value;
      } else {
        return new object[] { value };
      }
    }

    /// <summary>
    /// Update the KeyMappings with their real values.
    /// </summary>
    /// <returns></returns>
    protected List<KeyMapping> UpdateAutoGeneratedKeys(List<EntityInfo> entitiesWithAutoGeneratedKeys) {
      var list = new List<KeyMapping>();
      foreach (var entityInfo in entitiesWithAutoGeneratedKeys) {
        KeyMapping km;
        if (EntityKeyMapping.TryGetValue(entityInfo, out km)) {
          if (km.TempValue != null) {
            var entity = entityInfo.Entity;
            var id = GetIdentifier(entity);
            km.RealValue = id;
            list.Add(km);
          }
        }
      }
      return list;
    }

    /// <summary>
    /// Refresh the entities from the database.  This picks up changes due to triggers, etc.
    /// </summary>
    /// TODO make this faster
    /// TODO make this optional
    /// <param name="saveMap"></param>
    protected void RefreshFromSession(Dictionary<Type, List<EntityInfo>> saveMap) {
      //using (var tx = session.BeginTransaction()) {
      foreach (var kvp in saveMap) {
        foreach (var entityInfo in kvp.Value) {
          if (entityInfo.EntityState == EntityState.Added || entityInfo.EntityState == EntityState.Modified)
            session.Refresh(entityInfo.Entity);
        }
      }
      //tx.Commit();
      //}
    }


    #endregion
  }

}
