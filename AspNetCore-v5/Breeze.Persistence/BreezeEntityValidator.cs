using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Breeze.Persistence {
  /// <summary>
  /// Validates entities using Breeze metadata.  This duplicates the validation performed on the Breeze client.
  /// </summary>
  public class BreezeEntityValidator {
    private PersistenceManager _persistenceManager;
    private Dictionary<string, StructuralType> _structuralTypeMap;

    /// <summary>
    /// Create a new instance.  
    /// </summary>
    /// <param name="persistenceManager">Used for getting entity keys for building EntityError objects.</param>
    /// <param name="structuralTypeList">Contains the validator information for properties of entity and complex types.</param>
    public BreezeEntityValidator(PersistenceManager persistenceManager, List<MetaType> structuralTypeList) {
      this._persistenceManager = persistenceManager;
      this._structuralTypeMap = BuildStructuralTypeMap(structuralTypeList);
    }

    /// <summary>
    /// Create a new instance.  
    /// </summary>
    /// <param name="persistenceManager">Used for getting entity keys for building EntityError objects.</param>
    /// <param name="breezeMetadata">Contains breeze metadata. The structuralTypeList is extracted from it.</param>
    public BreezeEntityValidator(PersistenceManager persistenceManager, BreezeMetadata breezeMetadata) {
      this._persistenceManager = persistenceManager;
      var structuralTypeList = breezeMetadata.StructuralTypes;
      this._structuralTypeMap = BuildStructuralTypeMap(structuralTypeList);
    }


    /// <summary>
    /// Validate all the entities in the saveMap.
    /// </summary>
    /// <param name="saveMap">Map of type to entities.</param>
    /// <param name="throwIfInvalid">If true, throws an EntityErrorsException if any entity is invalid</param>
    /// <exception cref="EntityErrorsException">Contains all the EntityErrors.  Only thrown if throwIfInvalid is true.</exception>
    /// <returns>List containing an EntityError for each failed validation.</returns>
    public List<EntityError> ValidateEntities(Dictionary<Type, List<EntityInfo>> saveMap, bool throwIfInvalid) {
      var entityErrors = new List<EntityError>();
      foreach (var kvp in saveMap) {

        foreach (var entityInfo in kvp.Value) {
          ValidateEntity(entityInfo, entityErrors);
        }
      }
      if (throwIfInvalid && entityErrors.Any()) {
        throw new EntityErrorsException(entityErrors);
      }
      return entityErrors;
    }

    /// <summary>
    /// Validates a single entity.
    /// Skips validation (returns true) if entity is marked Deleted.
    /// </summary>
    /// <param name="entityInfo">contains the entity to validate</param>
    /// <param name="entityErrors">An EntityError is added to this list for each error found in the entity</param>
    /// <returns>true if entity is valid, false if invalid.</returns>
    public bool ValidateEntity(EntityInfo entityInfo, List<EntityError> entityErrors) {
      if (entityInfo.EntityState == EntityState.Deleted) return true;
      bool isValid = true;
      var entity = entityInfo.Entity;
      var entityType = entity.GetType();
      var entityTypeName = entityType.FullName;
      var sType = _structuralTypeMap[entityTypeName];
      var dataProperties = sType.dataProperties;
      object[] keyValues = null;
      foreach (var dp in sType.dataProperties) {
        if (dp.validators == null || dp.validators.Count == 0) continue;
        if (dp.propertyInfo == null) {
          dp.propertyInfo = entityType.GetProperty(dp.metaDataProperty.NameOnServer);  // try converting from camelCase?
          if (dp.propertyInfo == null) continue;
        }
        var value = dp.propertyInfo.GetValue(entity, null);

        foreach (var validator in dp.validators) {
          var errorMessage = validator.Validate(value);
          if (errorMessage != null) {
            if (keyValues == null) keyValues = _persistenceManager.GetKeyValues(entityInfo);

            entityErrors.Add(new EntityError() {
              EntityTypeName = entityTypeName,
              ErrorMessage = errorMessage,
              ErrorName = "ValidationError",
              KeyValues = keyValues,
              PropertyName = dp.metaDataProperty.NameOnServer
            });
            isValid = false;
          }
        }
      }
      return isValid;
    }

    #region Build the validator structure
    private Dictionary<string, StructuralType> BuildStructuralTypeMap(List<MetaType> structuralTypeList) {
      var map = new Dictionary<string, StructuralType>();
      foreach (var dt in structuralTypeList) {
        var st = new StructuralType();
        st.fullName = dt.Namespace + '.' + dt.ShortName;
        st.dataProperties = new List<DataProperty>();
        foreach (var dpo in dt.DataProperties) {
          st.dataProperties.Add(BuildDataProperty(dpo));
        }
        map.Add(st.fullName, st);
      }
      return map;
    }

    private DataProperty BuildDataProperty(MetaDataProperty data) {
      var dp = new DataProperty(data);
      //dp.dataType = data.DataType ?? data.ComplexTypeName;
      //dp.isNullable = data.IsNullable ?? true;

      if (data.Validators != null && data.Validators.Count > 0) {
        dp.validators = new List<IValidator>();
        foreach (var vd in data.Validators) {
          dp.validators.Add(BuildValidator(vd));
        }
      }
      return dp;
    }

    private IValidator BuildValidator(MetaValidator data) {
      var name = data.Name;
      IValidator v;
      if (name == MetaValidator.Required.Name) {
        v = new RequiredValidatorImpl();
      } else if (data is MaxLengthMetaValidator) {
        v = new MaxLengthValidatorImpl(((MaxLengthMetaValidator)data).MaxLength);
      } else {
        v = new MetaValidatorImpl(data.Name);
      }
      //if (data.Count > 1) {
      //  v.properties = data;
      //}
      return v;
    }
    #endregion
  }

  interface IValidator {
    string Name { get; set; }
    string Validate(object value);
  }

  #region Internal types
  internal class StructuralType {
    internal string fullName;
    internal List<DataProperty> dataProperties;
  }

  internal class DataProperty {
    internal MetaDataProperty metaDataProperty;
    internal PropertyInfo propertyInfo;
    internal List<IValidator> validators;
    public DataProperty(MetaDataProperty source) {
      this.metaDataProperty = source;
    }
  }
  internal class MetaValidatorImpl : MetaValidator, IValidator {
    public MetaValidatorImpl(string name) : base(name) { }
    public virtual string Validate(object value) {
      return null;
    }
  }

  internal class RequiredValidatorImpl : MetaValidator, IValidator {
    public RequiredValidatorImpl() : base("required") { }
    public virtual string Validate(object value) {
      if (value == null) {
        return "A value is required.";
      }
      var s = value as string;
      if (s != null && String.IsNullOrWhiteSpace(s)) {
        return "A value is required.";
      }
      return null;
    }
  }

  internal class MaxLengthValidatorImpl : MaxLengthMetaValidator, IValidator {
    public MaxLengthValidatorImpl(int maxLength) : base(maxLength) { }
    public virtual string Validate(object value) {
      if (MaxLength < 0) throw new Exception("Validator maxLength must be >= 0");
      var s = value as string;
      if (s == null) return null;
      if (s.Length > MaxLength) {
        return "String length must not exceed " + MaxLength;
      }
      return null;
    }
  }
  // TODO data type validators
  // TODO custom validators
  #endregion
}
