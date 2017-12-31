using Breeze.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Breeze.Persistence.EFCore {

  public interface IEFContextProvider {
    DbContext DbContext { get; }
    String GetEntitySetName(Type entityType);
  }

  // T is a subclass of DbContext 
  public class EFPersistenceManager<T> : PersistenceManager, IEFContextProvider where T : DbContext {

    private T _context;

    static EFPersistenceManager() {
      EntityQuery.ApplyExpand = EFExtensions.ApplyExpand;
    }

    public EFPersistenceManager() {
      _context = null;
    }

    public EFPersistenceManager(T context) {
      _context = context;
    }

    public DbContext DbContext {
      get {
        return _context;
      }
    }

    public T Context {
      get {
        return _context;
      }
    }

    //protected virtual T CreateContext() {
    //  throw new NotImplementedException("A CreateContext method must be implemented - if you do not instantiate this PersistenceManager with a Context"); ;
    //}



    /// <summary>Gets the EntityConnection from the ObjectContext.</summary>
    public DbConnection EntityConnection {
      get {
        return (DbConnection)GetDbConnection();
      }
    }

    /// <summary>Gets the current transaction, if one is in progress.</summary>
    public IDbContextTransaction EntityTransaction {
      get; private set;
    }


    ///// <summary>Gets the EntityConnection from the ObjectContext.</summary>
    public override IDbConnection GetDbConnection() {
      return DbContext.Database.GetDbConnection();
    }

    /// <summary>
    /// Opens the DbConnection used by the Context.
    /// If the connection will be used outside of the DbContext, this method should be called prior to DbContext 
    /// initialization, so that the connection will already be open when the DbContext uses it.  This keeps
    /// the DbContext from closing the connection, so it must be closed manually.
    /// See http://blogs.msdn.com/b/diego/archive/2012/01/26/exception-from-dbcontext-api-entityconnection-can-only-be-constructed-with-a-closed-dbconnection.aspx
    /// </summary>
    /// <returns></returns>
    protected override void OpenDbConnection() {
      DbContext.Database.OpenConnection();

    }

    protected override void CloseDbConnection() {
      if (_context != null) {
        DbContext.Database.CloseConnection();
      }
    }

    // Override BeginTransaction so we can keep the current transaction in a property
    protected override IDbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel) {
      var conn = GetDbConnection();
      if (conn == null) return null;
      EntityTransaction = DbContext.Database.BeginTransaction(isolationLevel);
      return EntityTransaction.GetDbTransaction();

    }


    #region Base implementation overrides

    protected override string BuildJsonMetadata() {
      var metadata = GetMetadataFromContext(DbContext);
      var jss = new JsonSerializerSettings {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
      };
      jss.Converters.Add(new StringEnumConverter());

      var json = JsonConvert.SerializeObject(metadata, jss);

      var altMetadata = BuildAltJsonMetadata();
      if (altMetadata != null) {
        json = "{ \"altMetadata\": " + altMetadata + "," + json.Substring(1);
      }
      return json;
    }

    private BreezeMetadata GetMetadataFromContext(DbContext dbContext) {
      var metadata = new BreezeMetadata();
      var dbSetMap = GetDbSetMap(dbContext);
      metadata.StructuralTypes = dbContext.Model.GetEntityTypes()
        .Where(et => !et.IsOwned())
        .Select(et => CreateMetaType(et, dbSetMap)).ToList();

      
      var complexTypes = dbContext.Model.GetEntityTypes()
        .Where(et => et.IsOwned()) 
        .Select(et => CreateMetaType(et,  dbSetMap)).ToList();
      // Complex types show up once per parent reference and we need to reduce
      // this to just the unique types.
      var complexTypesMap = complexTypes.ToDictionary(mt => mt.ShortName);
      complexTypesMap.Values.ToList().ForEach(v => metadata.StructuralTypes.Add(v));
      

      //complexTypes.ToList().ForEach(ct => {
      //  var mt = new MetaType();
      //  mt.ShortName = ct.Name;
      //  mt.Namespace = ct.Namespace;
      //  mt.IsComplexType = true;
      //  mt.DataProperties = ct.GetProperties().ToList().Select(pi => {
      //    var dp = new MetaDataProperty();

      //    dp.NameOnServer = pi.Name;
      //    dp.IsNullable = TypeFns.IsNullableType(pi.PropertyType);

      //    // dp.MaxLength = ???
      //    dp.DataType = NormalizeDataTypeName(pi.PropertyType);
      //    dp.AddValidators(pi.PropertyType);
      //    return dp;
      //  }).ToList();
      //  metadata.StructuralTypes.Add(mt);
      //});
      return metadata;
    }

    public Dictionary<Type, String> GetDbSetMap(DbContext context) {
      var dbSetProperties = new List<PropertyInfo>();
      var properties = context.GetType().GetProperties();
      var result = new Dictionary<Type, String>();
      foreach (var property in properties) {
        var setType = property.PropertyType;
        var isDbSet = setType.IsGenericType && (typeof(DbSet<>).IsAssignableFrom(setType.GetGenericTypeDefinition()));
        if (isDbSet) {
          var entityType = setType.GetGenericArguments()[0];
          var resourceName = property.Name;
          result.Add(entityType, resourceName);
        }
      }
      return result;
    }

    private MetaType CreateMetaType(Microsoft.EntityFrameworkCore.Metadata.IEntityType et, Dictionary<Type, String> dbSetMap) {
      var mt = new MetaType();
      mt.ShortName = et.ClrType.Name;
      mt.Namespace = et.ClrType.Namespace;
      if (et.IsOwned()) {
        mt.IsComplexType = true;
      }
      String resourceName;
      if (dbSetMap.TryGetValue(et.ClrType, out resourceName)) {
        mt.DefaultResourceName = resourceName;
      }

      var hasIdentityColumn = false;

      mt.DataProperties = et.GetProperties().Select(p => {
        var dp = new MetaDataProperty();

        dp.NameOnServer = p.Name;
        dp.IsNullable = p.IsNullable;
        dp.IsPartOfKey = p.IsPrimaryKey() ? true : (bool?)null;
        hasIdentityColumn = hasIdentityColumn || (p.IsPrimaryKey() && p.ValueGenerated == ValueGenerated.OnAdd);
        dp.MaxLength = p.GetMaxLength();
        dp.DataType = NormalizeDataTypeName(p.ClrType);
        dp.ConcurrencyMode = p.IsConcurrencyToken ? "Fixed" : null;
        var dfa = p.GetAnnotations().Where(a => a.Name == "DefaultValue").FirstOrDefault();
        if (dfa != null) {
          dp.DefaultValue = dfa.Value;
        }
        dp.AddValidators(p.ClrType);
        return dp;
      }).ToList();
      // EF returns parent's key with the complex type - we need to remove this.
      if (mt.IsComplexType) {
        mt.DataProperties = mt.DataProperties.Where(dp => dp.IsPartOfKey == null).ToList();
      }

      if (!mt.IsComplexType) {
        mt.AutoGeneratedKeyType = hasIdentityColumn ? AutoGeneratedKeyType.Identity : AutoGeneratedKeyType.None;
      }

      // Handle complex properties
      // for now this only complex types ( 'owned types' in EF parlance are eager loaded)
      var ownedNavigations = et.GetNavigations().Where(n => n.GetTargetType().IsOwned());
      ownedNavigations.ToList().ForEach(n => {
        var complexType = n.GetTargetType().ClrType;
        // complexTypes.Add(complexType);
        var dp = new MetaDataProperty();
        dp.NameOnServer = n.Name;
        dp.IsNullable = false;
        dp.IsPartOfKey = false;
        dp.ComplexTypeName = NormalizeTypeName(complexType);
        mt.DataProperties.Add(dp);
      });

      mt.NavigationProperties = et.GetNavigations().Where(n => !n.GetTargetType().IsOwned()).Select(p => {
        var np = new MetaNavProperty();
        np.NameOnServer = p.Name;
        np.EntityTypeName = NormalizeTypeName(p.GetTargetType().ClrType);
        np.IsScalar = !p.IsCollection();
        // FK_<dependent type name>_<principal type name>_<foreign key property name>
        np.AssociationName = BuildAssocName(p);
        if (p.IsDependentToPrincipal()) {
          np.AssociationName = BuildAssocName(p);
          np.ForeignKeyNamesOnServer = p.ForeignKey.Properties.Select(fkp => fkp.Name).ToList();
        } else {
          np.AssociationName = BuildAssocName(p.FindInverse());
          np.InvForeignKeyNamesOnServer = p.ForeignKey.Properties.Select(fkp => fkp.Name).ToList();
        }

        return np;
      }).ToList();
      return mt;
    }
    private string BuildAssocName(INavigation prop) {
      var assocName = prop.DeclaringEntityType.Name + "_" + prop.GetTargetType().Name + "_" + prop.Name;
      return assocName;
    }

    private string NormalizeTypeName(Type type) {
      return type.Name + ":#" + type.Namespace;
    }

    private string NormalizeDataTypeName(Type type) {
      type = TypeFns.GetNonNullableType(type);
      var result = type.ToString().Replace("System.", "");
      if (result == "Byte[]") {
        return "Binary";
      } else {
        return result;
      }
    }

    protected virtual string BuildAltJsonMetadata() {
      // default implementation
      return null; // "{ \"foo\": 8, \"bar\": \"xxx\" }";
    }

    protected override EntityInfo CreateEntityInfo() {
      return new EFEntityInfo();
    }

    public override object[] GetKeyValues(EntityInfo entityInfo) {
      return GetKeyValues(entityInfo.Entity);
    }

    public object[] GetKeyValues(object entity) {
      var et = entity.GetType();
      var values = GetKeyProperties(et).Select(kp => kp.GetValue(entity)).ToArray();
      return values;
    }

    private IEnumerable<PropertyInfo> GetKeyProperties(Type entityType) {
      var pk = Context.Model.FindEntityType(entityType).FindPrimaryKey();
      var props = pk.Properties.Select(k => k.PropertyInfo);
      return props;
    }

    protected override void SaveChangesCore(SaveWorkState saveWorkState) {
      var saveMap = saveWorkState.SaveMap;
      var deletedEntities = ProcessSaves(saveMap);

      if (deletedEntities.Any()) {
        ProcessAllDeleted(deletedEntities);
      }
      ProcessAutogeneratedKeys(saveWorkState.EntitiesWithAutoGeneratedKeys);


      try {
        DbContext.SaveChanges();

        //} catch (DbEntityValidationException e) {
        //  var entityErrors = new List<EntityError>();
        //  foreach (var eve in e.EntityValidationErrors) {
        //    var entity = eve.Entry.Entity;
        //    var entityTypeName = entity.GetType().FullName;
        //    Object[] keyValues;
        //    var key = GetEntityEntry(entity).EntityKey;
        //    if (key.EntityKeyValues != null) {
        //      keyValues = key.EntityKeyValues.Select(km => km.Value).ToArray();
        //    } else {
        //      var entityInfo = saveWorkState.EntitiesWithAutoGeneratedKeys.FirstOrDefault(ei => ei.Entity == entity);
        //      if (entityInfo != null) {
        //        keyValues = new Object[] { entityInfo.AutoGeneratedKey.TempValue };
        //      } else {
        //        // how can this happen?
        //        keyValues = null;
        //      }
        //    }
        //    foreach (var ve in eve.ValidationErrors) {
        //      var entityError = new EntityError() {
        //        EntityTypeName = entityTypeName,
        //        KeyValues = keyValues,
        //        ErrorMessage = ve.ErrorMessage,
        //        PropertyName = ve.PropertyName
        //      };
        //      entityErrors.Add(entityError);
        //    }

        //  }
        //  saveWorkState.EntityErrors = entityErrors;

      } catch (DbUpdateException e) {
        var nextException = (Exception)e;
        while (nextException.InnerException != null) {
          nextException = nextException.InnerException;
        }
        if (nextException == e) {
          throw;
        } else {
          //create a new exception that contains the toplevel exception
          //but has the innermost exception message propogated to the top.
          //For EF exceptions, this is often the most 'relevant' message.
          throw new Exception(nextException.Message, e);
        }
      } catch (Exception) {
        throw;
      }

      saveWorkState.KeyMappings = UpdateAutoGeneratedKeys(saveWorkState.EntitiesWithAutoGeneratedKeys);
    }

    #endregion

    #region Save related methods

    private List<EFEntityInfo> ProcessSaves(Dictionary<Type, List<EntityInfo>> saveMap) {
      var deletedEntities = new List<EFEntityInfo>();
      foreach (var kvp in saveMap) {
        if (kvp.Value == null || kvp.Value.Count == 0) continue;  // skip GetEntitySetName if no entities
        var entityType = kvp.Key;
        var entitySetName = GetEntitySetName(entityType);
        foreach (EFEntityInfo entityInfo in kvp.Value) {
          // entityInfo.EFContextProvider = this;  may be needed eventually.
          entityInfo.EntitySetName = entitySetName;
          ProcessEntity(entityInfo);
          if (entityInfo.EntityState == EntityState.Deleted) {
            deletedEntities.Add(entityInfo);
          }
        }
      }
      return deletedEntities;
    }

    private void ProcessAllDeleted(List<EFEntityInfo> deletedEntities) {
      deletedEntities.ForEach(entityInfo => {

        RestoreOriginal(entityInfo);
        var entry = GetOrAddEntityEntry(entityInfo);
        entry.State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
        entityInfo.EntityEntry = entry;
      });
    }

    private void ProcessAutogeneratedKeys(List<EntityInfo> entitiesWithAutoGeneratedKeys) {
      var tempKeys = entitiesWithAutoGeneratedKeys.Cast<EFEntityInfo>().Where(
        entityInfo => entityInfo.AutoGeneratedKey.AutoGeneratedKeyType == AutoGeneratedKeyType.KeyGenerator)
        .Select(ei => new TempKeyInfo(ei))
        .ToList();
      if (tempKeys.Count == 0) return;
      if (this.KeyGenerator == null) {
        this.KeyGenerator = GetKeyGenerator();
      }
      this.KeyGenerator.UpdateKeys(tempKeys);
      tempKeys.ForEach(tki => {
        // Clever hack - next 3 lines cause all entities related to tki.Entity to have 
        // their relationships updated. So all related entities for each tki are updated.
        // Basically we set the entity to look like a preexisting entity by setting its
        // entityState to unchanged.  This is what fixes up the relations, then we set it back to added
        // Now when we update the pk - all fks will get changed as well.  Note that the fk change will only
        // occur during the save.
        var entry = GetEntityEntry(tki.Entity);
        entry.State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
        entry.State = Microsoft.EntityFrameworkCore.EntityState.Added;
        var val = ConvertValue(tki.RealValue, tki.Property.PropertyType);
        tki.Property.SetValue(tki.Entity, val, null);
      });
    }

    private IKeyGenerator GetKeyGenerator() {
      var generatorType = KeyGeneratorType.Value;
      return (IKeyGenerator)Activator.CreateInstance(generatorType, this.GetDbConnection());
    }

    private EntityInfo ProcessEntity(EFEntityInfo entityInfo) {
      EntityEntry ose;
      if (entityInfo.EntityState == EntityState.Modified) {
        ose = HandleModified(entityInfo);
      } else if (entityInfo.EntityState == EntityState.Added) {
        ose = HandleAdded(entityInfo);
      } else if (entityInfo.EntityState == EntityState.Deleted) {
        // for 1st pass this does NOTHING 
        ose = HandleDeletedPart1(entityInfo);
      } else {
        // needed for many to many to get both ends into the objectContext
        ose = HandleUnchanged(entityInfo);
      }
      entityInfo.EntityEntry = ose;
      return entityInfo;
    }

    private EntityEntry HandleAdded(EFEntityInfo entityInfo) {
      var entry = AddEntityEntry(entityInfo);
      if (entityInfo.AutoGeneratedKey != null) {
        var propName = entityInfo.AutoGeneratedKey.PropertyName;
        entityInfo.AutoGeneratedKey.TempValue = GetEntryPropertyValue(entry, propName);
        if (entityInfo.AutoGeneratedKey.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity) {
          // HACK: because EF Core will not allow inserts to an Identity column where a value exists on incoming entity.
          entry.Property(propName).IsTemporary = true;
        }
      }
            
      entry.State = Microsoft.EntityFrameworkCore.EntityState.Added;
      return entry;
    }

    private EntityEntry HandleModified(EFEntityInfo entityInfo) {
      var entry = AddEntityEntry(entityInfo);
      // EntityState will be changed to modified during the update from the OriginalValuesMap
      // Do NOT change this to EntityState.Modified because this will cause the entire record to update.
      entry.State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;

      // updating the original values is necessary under certain conditions when we change a foreign key field
      // because the before value is used to determine ordering.
      UpdateOriginalValues(entry, entityInfo);

      SetModified(entry, entityInfo.ForceUpdate);

      // Handle owned entities - ( complex types).
      var ownedNavs = entry.Navigations.Where(n => n.Metadata.GetTargetType().IsOwned());
      ownedNavs.ToList().ForEach(n => {
        var nEntry = GetEntityEntry(n.CurrentValue);
        SetModified(nEntry, entityInfo.ForceUpdate);
      });

      return entry;
    }

    private void SetModified(EntityEntry entry, bool force = false) {
      if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Modified || force) {
        // _originalValusMap can be null if we mark entity.SetModified but don't actually change anything.
        entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
      }
    }

    private EntityEntry HandleUnchanged(EFEntityInfo entityInfo) {
      var entry = AddEntityEntry(entityInfo);
      entry.State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
      return entry;
    }

    private EntityEntry HandleDeletedPart1(EntityInfo entityInfo) {
      return null;
    }

    private EntityInfo RestoreOriginal(EntityInfo entityInfo) {
      // fk's can get cleared depending on the order in which deletions occur -
      // EF needs the original values of these fk's under certain circumstances - ( not sure entirely what these are). 
      // so we restore the original fk values right before we attach the entity 
      // shouldn't be any side effects because we delete it immediately after.
      // ??? Do concurrency values also need to be restored in some cases 
      // This method restores more than it actually needs to because we don't
      // have metadata easily avail here, but usually a deleted entity will
      // not have much in the way of OriginalValues.
      if (entityInfo.OriginalValuesMap == null || entityInfo.OriginalValuesMap.Keys.Count == 0) {
        return entityInfo;
      }
      var entity = entityInfo.Entity;
      var entityType = entity.GetType();
      
      var keyPropertyNames = GetKeyProperties(entityType).Select(kp => kp.Name).ToList();
      var ovl = entityInfo.OriginalValuesMap.ToList();
      for (var i = 0; i < ovl.Count; i++) {
        var kvp = ovl[i];
        var propName = kvp.Key;
        // keys should be ignored
        if (keyPropertyNames.Contains(propName)) continue;
        var pi = entityType.GetProperty(propName);
        // unmapped properties should be ignored.
        if (pi == null) continue;
        var nnPropType = TypeFns.GetNonNullableType(pi.PropertyType);
        // presumption here is that only a predefined type could be a fk or concurrency property
        if (TypeFns.IsPredefinedType(nnPropType)) {
          SetPropertyValue(entity, propName, kvp.Value);
        }
      }

      return entityInfo;
    }

    //private static EntityType GetEntityType(MetadataWorkspace mws, Type entityType) {
    //  EntityType et =
    //    mws.GetItems<EntityType>(DataSpace.OSpace)
    //      .Single(x => x.Name == entityType.Name);
    //  return et;
    //}

    private static void UpdateOriginalValues(EntityEntry entry, EntityInfo entityInfo) {
      var originalValuesMap = entityInfo.OriginalValuesMap;
      if (originalValuesMap == null || originalValuesMap.Keys.Count == 0) return;



      originalValuesMap.ToList().ForEach(kvp => {
        var propertyName = kvp.Key;
        var originalValue = kvp.Value;
        if (originalValue is JObject) {
          // only really need to perform updating original values on key properties
          // and a complex object cannot be a key.
          return;
        }

        try {
          var propEntry = entry.Property(propertyName);
          propEntry.IsModified = true;
          var fieldType = propEntry.Metadata.ClrType;
          var originalValueConverted = ConvertValue(originalValue, fieldType);
          propEntry.OriginalValue = originalValueConverted;

        } catch (Exception e) {
          if (e.Message.Contains(" part of the entity's key")) {
            throw;
          } else {
            // this can happen for "custom" data entity properties.
          }
        }
      });

    }

    private List<KeyMapping> UpdateAutoGeneratedKeys(List<EntityInfo> entitiesWithAutoGeneratedKeys) {
      // where clause is necessary in case the Entities were suppressed in the beforeSave event.
      var keyMappings = entitiesWithAutoGeneratedKeys.Cast<EFEntityInfo>()
        .Where(entityInfo => entityInfo.EntityEntry != null)
        .Select((Func<EFEntityInfo, KeyMapping>)(entityInfo => {
          var autoGeneratedKey = entityInfo.AutoGeneratedKey;
          if (autoGeneratedKey.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity) {
            autoGeneratedKey.RealValue = GetEntryPropertyValue(entityInfo.EntityEntry, autoGeneratedKey.PropertyName);
          }
          return new KeyMapping() {
            EntityTypeName = entityInfo.Entity.GetType().FullName,
            TempValue = autoGeneratedKey.TempValue,
            RealValue = autoGeneratedKey.RealValue
          };
        }));
      return keyMappings.ToList();
    }

    private Object GetEntryPropertyValue(EntityEntry entry, String propertyName) {
      var currentValues = entry.CurrentValues;
      return currentValues[propertyName];

    }

    private void SetEntryPropertyValue(EntityEntry entry, String propertyName, Object value) {
      var currentValues = entry.CurrentValues;
      currentValues[propertyName] = value;
    }

    private void SetPropertyToDefaultValue(Object entity, String propertyName) {
      var propInfo = entity.GetType().GetProperty(propertyName,
                                                  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      // exit if unmapped property.
      if (propInfo == null) return;
      if (propInfo.CanWrite) {
        var val = TypeFns.GetDefaultValue(propInfo.PropertyType);
        propInfo.SetValue(entity, val, null);
      } else {
        throw new Exception(String.Format("Unable to write to property '{0}' on type: '{1}'", propertyName,
                                          entity.GetType()));
      }

    }

    private void SetPropertyValue(Object entity, String propertyName, Object value) {
      var propInfo = entity.GetType().GetProperty(propertyName,
                                                  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      // exit if unmapped property.
      if (propInfo == null) return;
      if (propInfo.CanWrite) {
        var val = ConvertValue(value, propInfo.PropertyType);
        propInfo.SetValue(entity, val, null);
      } else {
        throw new Exception(String.Format("Unable to write to property '{0}' on type: '{1}'", propertyName,
                                          entity.GetType()));
      }
    }

    private static Object ConvertValue(Object val, Type toType) {
      Object result;
      if (val == null) return val;
      if (toType == val.GetType()) return val;
      var nnToType = TypeFns.GetNonNullableType(toType);
      if (typeof(IConvertible).IsAssignableFrom(nnToType)) {
        result = Convert.ChangeType(val, nnToType, System.Threading.Thread.CurrentThread.CurrentCulture);
      } else if (val is JObject) {
        var serializer = new JsonSerializer();
        result = serializer.Deserialize(new JTokenReader((JObject)val), toType);
      } else {
        // Guids fail above - try this
        TypeConverter typeConverter = TypeDescriptor.GetConverter(toType);
        if (typeConverter.CanConvertFrom(val.GetType())) {
          result = typeConverter.ConvertFrom(val);
        } else if (val is DateTime && toType == typeof(DateTimeOffset)) {
          // handle case where JSON deserializes to DateTime, but toType is DateTimeOffset.  DateTimeOffsetConverter doesn't work!
          result = new DateTimeOffset((DateTime)val);
        } else {
          result = val;
        }
      }
      return result;
    }

    private EntityEntry GetOrAddEntityEntry(EFEntityInfo entityInfo) {
      return AddEntityEntry(entityInfo);
    }

    private EntityEntry AddEntityEntry(EFEntityInfo entityInfo) {
      var entry = GetEntityEntry(entityInfo.Entity);
      if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached) {
        return DbContext.Add(entityInfo.Entity);
      } else {
        return entry;
      }

    }

    private EntityEntry AttachEntityEntry(EFEntityInfo entityInfo) {
      return DbContext.Attach(entityInfo.Entity);
    }

    private EntityEntry GetEntityEntry(EFEntityInfo entityInfo) {
      return GetEntityEntry(entityInfo.Entity);
    }

    private EntityEntry GetEntityEntry(Object entity) {
      var entry = this.DbContext.Entry(entity);
      return entry;
    }


    #endregion


    public String GetEntitySetName(Type entityType) {
      return this.Context.Model.FindEntityType(entityType).Name;
    }

  }

  public class EFEntityInfo : EntityInfo {
    internal EFEntityInfo() {
    }

    internal String EntitySetName;
    internal EntityEntry EntityEntry;
  }

  public class EFEntityError : EntityError {
    public EFEntityError(EntityInfo entityInfo, String errorName, String errorMessage, String propertyName) {


      if (entityInfo != null) {
        this.EntityTypeName = entityInfo.Entity.GetType().FullName;
        this.KeyValues = GetKeyValues(entityInfo);
      }
      ErrorName = errorName;
      ErrorMessage = errorMessage;
      PropertyName = propertyName;
    }

    private Object[] GetKeyValues(EntityInfo entityInfo) {
      return entityInfo.ContextProvider.GetKeyValues(entityInfo);
    }
  }




}