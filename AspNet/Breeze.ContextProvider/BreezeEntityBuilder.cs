using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters;

namespace Breeze.Entities
{
    /// <summary>
    /// Builds server-side entities from JSON results sent by the Breeze client.
    /// This class is thread-safe and can be used to handle multiple requests.
    /// </summary>
    public class BreezeEntityBuilder
    {
        public static readonly JsonSerializer jsonSerializer = JsonSerializer.Create(CreateJsonSerializerSettings());
        private readonly List<Assembly> modelAssemblies;

        /// <summary>
        /// Create an instance using a list of assemblies to probe for entity types that match the entities from the client.
        /// </summary>
        /// <param name="assemblies">List of assemblies to probe for entity types</param>
        public BreezeEntityBuilder(List<Assembly> assemblies)
        {
            this.modelAssemblies = assemblies;
        }

        /// <summary>
        /// Create an instance using a string to match the assembly names.  Matching assemblies will be probed for entity types that match the entities from the client.
        /// </summary>
        /// <param name="assemblyNameContains">String contained in the assembly name</param>
        public BreezeEntityBuilder(string assemblyNameContains)
        {
            this.modelAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains(assemblyNameContains)).ToList();
        }

        /// <summary>
        /// Create a SaveWorkState from the JObject sent by the client.
        /// </summary>
        /// <param name="saveBundle">JObject deserialized from Breeze client</param>
        /// <returns></returns>
        public SaveWorkState BuildSaveWorkState(JObject saveBundle)
        {
            var dynSaveBundle = (dynamic)saveBundle;
            var entitiesArray = (JArray)dynSaveBundle.entities;
            var rawEntities = entitiesArray.Select(jt => (dynamic)jt).ToList();
            return BuildSaveWorkState(rawEntities);
        }

        /// <summary>
        /// Create a SaveWorkState from the raw entities sent by the client.
        /// </summary>
        /// <param name="rawEntities">Objects assumed to have an entityAspect property containing the entityTypeName</param>
        /// <returns></returns>
        public SaveWorkState BuildSaveWorkState(List<object> rawEntities)
        {
            var entityGroups = BuildEntityGroups(rawEntities);
            var workState = new SaveWorkState(entityGroups);
            return workState;
        }

        /// <summary>
        /// Create EntityGroup objects from the raw entities sent by the client.
        /// </summary>
        /// <param name="rawEntities">Objects assumed to have an entityAspect property containing the entityTypeName</param>
        /// <returns></returns>
        protected List<EntityGroup> BuildEntityGroups(List<object> rawEntities)
        {
            var jObjects = rawEntities.Select(jt => (dynamic)jt).ToList();
            // group the objects by the entityTypeName
            var groups = jObjects.GroupBy(jo => (String)jo.entityAspect.entityTypeName).ToList();

            // create EntityGroup objects by looking up the entity type and creating the entity instances
            var entityInfoGroups = groups.Select(g => {
                var entityType = LookupEntityType(g.Key);
                var entityInfos = g.Select(jo => CreateEntityInfoFromJson(jo, entityType)).Cast<EntityInfo>().ToList();
                return new EntityGroup() { EntityType = entityType, EntityInfos = entityInfos };
            }).ToList();

            return entityInfoGroups;
        }

        /// <summary>
        /// Get the C# type from the entity type name.  Uses the modelAssemblies property to look up the entities.
        /// </summary>
        /// <param name="entityTypeName">Name in the form "Customer:#My.App.Namespace"</param>
        /// <returns></returns>
        protected Type LookupEntityType(string entityTypeName)
        {
            var delims = new string[] { ":#" };
            var parts = entityTypeName.Split(delims, StringSplitOptions.None);
            var shortName = parts[0];
            var ns = parts[1];

            var typeName = ns + "." + shortName;
            var type = modelAssemblies
              .Select(a => a.GetType(typeName, false, true))
              .FirstOrDefault(t => t != null);

            if (type != null)
                return type;
            else
                throw new ArgumentException("Assembly could not be found for " + entityTypeName);
        }

        /// <summary>
        /// Create an EntityInfo object from the raw client object
        /// </summary>
        /// <param name="jo">Object assumed to have an entityAspect property containing entityState and optional originalValuesMap and autoGeneratedKey</param>
        /// <param name="entityType">Domain model object type to create</param>
        /// <returns></returns>
        protected static EntityInfo CreateEntityInfoFromJson(dynamic jo, Type entityType)
        {
            var entityInfo = new EntityInfo();

            entityInfo.Entity = jsonSerializer.Deserialize(new JTokenReader(jo), entityType);
            entityInfo.EntityState = (EntityState)Enum.Parse(typeof(EntityState), (String)jo.entityAspect.entityState);

            entityInfo.UnmappedValuesMap = JsonToDictionary(jo.__unmapped);
            entityInfo.OriginalValuesMap = JsonToDictionary(jo.entityAspect.originalValuesMap);

            var autoGeneratedKey = jo.entityAspect.autoGeneratedKey;
            if (entityInfo.EntityState == EntityState.Added && autoGeneratedKey != null)
            {
                entityInfo.AutoGeneratedKey = new AutoGeneratedKey(entityInfo.Entity, autoGeneratedKey);
            }
            return entityInfo;
        }

        /// <summary>
        /// Convert the json object to a dictionary.
        /// </summary>
        /// <param name="json">Object assumed to be an IEnumerable containing JProperty objects</param>
        /// <returns></returns>
        protected static Dictionary<String, Object> JsonToDictionary(dynamic json)
        {
            if (json == null) return null;
            var jprops = ((System.Collections.IEnumerable)json).Cast<JProperty>();
            var dict = jprops.ToDictionary(jprop => jprop.Name, jprop => {
                var val = jprop.Value as JValue;
                if (val != null)
                {
                    return val.Value;
                }
                else if (jprop.Value as JArray != null)
                {
                    return jprop.Value as JArray;
                }
                else
                {
                    return jprop.Value as JObject;
                }
            });
            return dict;
        }

        /// <summary>
        /// Create breeze-friendly serializer settings
        /// </summary>
        /// <returns></returns>
        protected static JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Include,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            };

            jsonSerializerSettings.Converters.Add(new StringEnumConverter());
            return jsonSerializerSettings;
        }

    }

    /// <summary>
    /// Contains the entities and other data needed to conduct the save process.
    /// Each instance should only be used for a single save request.
    /// </summary>
    public class SaveWorkState
    {
        /// <summary>
        /// Populate the EntityInfoGroups, SaveMap, and EntitiesWithAutoGeneratedKeys
        /// </summary>
        /// <param name="entityInfoGroups"></param>
        public SaveWorkState(List<EntityGroup> entityInfoGroups)
        {
            this.EntityInfoGroups = entityInfoGroups;

            SaveMap = new Dictionary<Type, List<EntityInfo>>();
            EntityInfoGroups.ForEach(eg => {
                var entityInfos = eg.EntityInfos;
                SaveMap.Add(eg.EntityType, entityInfos);
            });
            EntitiesWithAutoGeneratedKeys = SaveMap
              .SelectMany(eiGrp => eiGrp.Value)
              .Where(ei => ei.AutoGeneratedKey != null)
              .ToList();
        }

        /// <summary>original entities materialized from the client</summary>
        protected List<EntityGroup> EntityInfoGroups;

        /// <summary>entities organized by Type; entities may be added or removed during the save process</summary>
        public Dictionary<Type, List<EntityInfo>> SaveMap { get; set; }

        /// <summary>entities that need their keys updated by the server</summary>
        public List<EntityInfo> EntitiesWithAutoGeneratedKeys { get; set; }

        /// <summary>mappings of temporary keys to server-generated keys.  Updated during the save process</summary>
        public List<KeyMapping> KeyMappings;

        /// <summary>Any business-rule errors generated during the save</summary>
        public List<EntityError> EntityErrors;

        /// <summary>Convert the SaveWorkState to a response for sending to the client.</summary>
        /// <returns></returns>
        public BreezeSaveResponse ToSaveResponse()
        {
            if (EntityErrors != null)
            {
                return new BreezeSaveResponse() { Errors = EntityErrors.Cast<Object>().ToList() };
            }
            else
            {
                var entities = SaveMap.SelectMany(kvp => kvp.Value.Select(entityInfo => entityInfo.Entity)).ToList();
                return new BreezeSaveResponse() { Entities = entities, KeyMappings = KeyMappings };
            }
        }
    }

    public enum EntityState
    {
        Detached = 1,
        Unchanged = 2,
        Added = 4,
        Deleted = 8,
        Modified = 16,
    }

    public class EntityGroup
    {
        public Type EntityType;
        public List<EntityInfo> EntityInfos;
    }

    /// <summary>
    /// Represents the entity object and the entityAspect data from the client
    /// </summary>
    public class EntityInfo
    {
        protected internal EntityInfo()
        {
        }

        public Object Entity { get; internal set; }
        public EntityState EntityState { get; set; }
        public bool ForceUpdate { get; set; }

        /// <summary>For entities with server-generated keys, holds the temporary client key and the real server-generated key.</summary>
        public AutoGeneratedKey AutoGeneratedKey { get; set; }

        /// <summary>Contains the original values of the properties that were changed on the client.</summary>
        public Dictionary<String, Object> OriginalValuesMap { get; set; }

        /// <summary>Contains additional client entity data that does not map to server entity properties</summary>
        public Dictionary<String, Object> UnmappedValuesMap { get; set; }
    }

    public enum AutoGeneratedKeyType
    {
        None,
        Identity,
        KeyGenerator
    }

    // Types returned to javascript as Json.
    //public class SaveResult
    //{
    //    public List<Object> Entities;
    //    public List<KeyMapping> KeyMappings;
    //    public List<Object> Errors;
    //}

    public class KeyMapping
    {
        public String EntityTypeName;
        public Object TempValue;
        public Object RealValue;
    }

    /// <summary>
    /// For entities with server-generated keys, holds the temporary client key and the real server-generated key.
    /// </summary>
    public class AutoGeneratedKey
    {
        public AutoGeneratedKey(Object entity, dynamic autoGeneratedKey)
        {
            Entity = entity;
            PropertyName = autoGeneratedKey.propertyName;
            AutoGeneratedKeyType = (AutoGeneratedKeyType)Enum.Parse(typeof(AutoGeneratedKeyType), (String)autoGeneratedKey.autoGeneratedKeyType);
            // TempValue and RealValue will be set later. - TempValue during Add, RealValue after save completes.
        }

        public Object Entity;
        public AutoGeneratedKeyType AutoGeneratedKeyType;
        public String PropertyName;
        public PropertyInfo Property
        {
            get
            {
                if (_property == null)
                {
                    _property = Entity.GetType().GetProperty(PropertyName,
                      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                return _property;
            }
        }
        public Object TempValue;
        public Object RealValue;
        private PropertyInfo _property;
    }

    /// <summary>
    /// Exception class for sending validation errors to higher levels of the application.
    /// </summary>
    public class EntityErrorsException : Exception
    {
        public EntityErrorsException(IEnumerable<EntityError> entityErrors)
        {
            EntityErrors = entityErrors.ToList();
            StatusCode = HttpStatusCode.Forbidden;
        }

        public EntityErrorsException(String message, IEnumerable<EntityError> entityErrors)
          : base(message)
        {
            EntityErrors = entityErrors.ToList();
            StatusCode = HttpStatusCode.Forbidden;
        }

        public HttpStatusCode StatusCode { get; set; }
        public List<EntityError> EntityErrors { get; protected set; }
    }

    /// <summary>
    /// Represents a validation error on a specific entity
    /// </summary>
    public class EntityError
    {
        /// <summary>Error type (may be used by client code for error display or categorization)</summary>
        public String ErrorName;

        /// <summary>Type of the entity on which the error occurred</summary>
        public String EntityTypeName;

        /// <summary>Property on which the error occurred.  May be blank if the error was not specific to one property.</summary>
        public String PropertyName;

        /// <summary>Entity key values to identify the entity on which the error occurred.</summary>
        public Object[] KeyValues;

        /// <summary>Message describing the error.</summary>
        public string ErrorMessage;
    }

    /// <summary>
    /// Wrapper class for sending validation errors to the client.
    /// </summary>
    public class SaveError
    {
        public SaveError(IEnumerable<EntityError> entityErrors)
        {
            EntityErrors = entityErrors.ToList();
        }
        public SaveError(String message, IEnumerable<EntityError> entityErrors)
        {
            Message = message;
            EntityErrors = entityErrors.ToList();
        }
        public String Message { get; protected set; }
        public List<EntityError> EntityErrors { get; protected set; }
    }

    public class BreezeSaveResponse
    {
        public List<Object> Entities { get; set; }
        public List<KeyMapping> KeyMappings { get; set; }
        public List<Object> Errors { get; set; }
    }


}