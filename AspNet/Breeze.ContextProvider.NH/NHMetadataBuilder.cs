using NHibernate;
using NHibernate.Engine;
using NHibernate.Id;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Breeze.ContextProvider.NH
{
    /// <summary>
    /// Builds a data structure containing the metadata required by Breeze.
    /// <see cref="http://www.breezejs.com/documentation/breeze-metadata-format"/>
    /// </summary>
    public class NHMetadataBuilder
    {
        private ISessionFactory _sessionFactory;
        private Metadata _map;
        private List<Dictionary<string, object>> _typeList;
        private Dictionary<string, object> _resourceMap;
        private HashSet<string> _typeNames;
        private List<Dictionary<string, object>> _enumList;

        public NHMetadataBuilder(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        /// <summary>
        /// Build the Breeze metadata as a nested Dictionary.  
        /// The result can be converted to JSON and sent to the Breeze client.
        /// </summary>
        /// <returns></returns>
        public Metadata BuildMetadata()
        {
            return BuildMetadata((Func<Type, bool>) null);
        }

        /// <summary>
        /// Build the Breeze metadata as a nested Dictionary.  
        /// The result can be converted to JSON and sent to the Breeze client.
        /// </summary>
        /// <param name="includeFilter">Function that returns true if a Type should be included in metadata, false otherwise</param>
        /// <returns></returns>
        public Metadata BuildMetadata(Func<Type, bool> includeFilter)
        {
            // retrieves all mappings with the name property set on the class  (mapping with existing type, no duck typing)
            IDictionary<string, IClassMetadata> classMeta = _sessionFactory.GetAllClassMetadata().Where(p => ((IEntityPersister)p.Value).EntityMetamodel.Type != null).ToDictionary(p => p.Key, p => p.Value);

            if (includeFilter != null)
            {
                classMeta = classMeta.Where(p => includeFilter(((IEntityPersister)p.Value).EntityMetamodel.Type)).ToDictionary(p => p.Key, p => p.Value);
            }
            return BuildMetadata(classMeta.Values);
        }

        /// <summary>
        /// Build the Breeze metadata as a nested Dictionary.  
        /// The result can be converted to JSON and sent to the Breeze client.
        /// </summary>
        /// <param name="classMeta">Entity metadata types to include in the metadata</param>
        /// <returns></returns>
        public Metadata BuildMetadata(IEnumerable<IClassMetadata> classMeta)
        {
            InitMap();

            foreach (var meta in classMeta)
            {
                AddClass(meta);
            }
            return _map;
        }

        /// <summary>
        /// Populate the metadata header.
        /// </summary>
        void InitMap()
        {
            _map = new Metadata();
            _typeList = new List<Dictionary<string, object>>();
            _typeNames = new HashSet<string>();
            _resourceMap = new Dictionary<string, object>();
            _map.ForeignKeyMap = new Dictionary<string, string>();
            _enumList = new List<Dictionary<string, object>>();
            _map.Add("localQueryComparisonOptions", "caseInsensitiveSQL");
            _map.Add("structuralTypes", _typeList);
            _map.Add("resourceEntityTypeMap", _resourceMap);
            _map.Add("enumTypes", _enumList);
        }

        /// <summary>
        /// Add the metadata for an entity.
        /// </summary>
        /// <param name="meta"></param>
        void AddClass(IClassMetadata meta)
        {
            var type = meta.GetMappedClass(EntityMode.Poco);

            // "Customer:#Breeze.Nhibernate.NorthwindIBModel": {
            var classKey = type.Name + ":#" + type.Namespace;
            var cmap = new Dictionary<string, object>();
            _typeList.Add(cmap);

            cmap.Add("shortName", type.Name);
            cmap.Add("namespace", type.Namespace);

            var entityPersister = meta as IEntityPersister;
            var metaModel = entityPersister.EntityMetamodel;
            var superType = metaModel.SuperclassType;
            if (superType != null)
            {
                var baseTypeName = superType.Name + ":#" + superType.Namespace;
                cmap.Add("baseTypeName", baseTypeName);
            }

            var generator = entityPersister != null ? entityPersister.IdentifierGenerator : null;
            if (generator != null)
            {
                string genType = null;
                if (generator is IdentityGenerator) genType = "Identity";
                else if (generator is Assigned || generator is ForeignGenerator) genType = "None";
                else genType = "KeyGenerator";
                cmap.Add("autoGeneratedKeyType", genType); // TODO find the real generator
            }

            var resourceName = Pluralize(type.Name); // TODO find the real name
            cmap.Add("defaultResourceName", resourceName);
            _resourceMap.Add(resourceName, classKey);

            var dataList = new List<Dictionary<string, object>>();
            cmap.Add("dataProperties", dataList);
            var navList = new List<Dictionary<string, object>>();
            cmap.Add("navigationProperties", navList);

            AddClassProperties(meta, dataList, navList);
        }

        /// <summary>
        /// Add the properties for an entity.
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="dataList">will be populated with the data properties of the entity</param>
        /// <param name="navList">will be populated with the navigation properties of the entity</param>
        void AddClassProperties(IClassMetadata meta, List<Dictionary<string, object>> dataList, List<Dictionary<string, object>> navList)
        {
            var persister = meta as AbstractEntityPersister;
            var metaModel = persister.EntityMetamodel;
            var type = metaModel.Type;

            HashSet<String> inheritedProperties = GetSuperProperties(persister);

            var propNames = meta.PropertyNames;
            var propTypes = meta.PropertyTypes;
            var propNull = meta.PropertyNullability;
            var properties = metaModel.Properties;
            for (int i = 0; i < propNames.Length; i++)
            {
                var propName = propNames[i];
                if (inheritedProperties.Contains(propName)) continue;  // skip property defined on superclass

                var propType = propTypes[i];
                if (!propType.IsAssociationType)    // skip association types until we handle all the data types, so they can be looked up
                {
                    if (propType.IsComponentType)
                    {
                        // complex type
                        var columnNames = persister.GetPropertyColumnNames(i);
                        var compType = (ComponentType)propType;
                        var complexTypeName = AddComponent(compType, columnNames);
                        var compMap = new Dictionary<string, object>();
                        compMap.Add("nameOnServer", propName);
                        compMap.Add("complexTypeName", complexTypeName);
                        compMap.Add("isNullable", propNull[i]);
                        dataList.Add(compMap);
                    }
                    else
                    {
                        // data property
                        var isKey = meta.HasNaturalIdentifier && meta.NaturalIdentifierProperties.Contains(i);
                        var isVersion = meta.IsVersioned && i == meta.VersionProperty;

                        var dmap = MakeDataProperty(propName, propType, propNull[i], isKey, isVersion);
                        dataList.Add(dmap);
                    }
                }

                // Expose enum types
                if (propType is AbstractEnumType)
                {
                    var types = propType.GetType().GetGenericArguments();
                    if (types.Length > 0)
                    {
                        var realType = types[0];
                        string[] enumNames = Enum.GetNames(realType);
                        var p = new Dictionary<string, object>();
                        p.Add("shortName", realType.Name);
                        p.Add("namespace", realType.Namespace);
                        p.Add("values", enumNames);
                        if (!_enumList.Exists(x => x.ContainsValue(realType.Name)))
                        {
                            _enumList.Add(p);
                        }
                    }
                }
            }


            // Hibernate identifiers are excluded from the list of data properties, so we have to add them separately
            if (meta.HasIdentifierProperty && !inheritedProperties.Contains(meta.IdentifierPropertyName))
            {
                var dmap = MakeDataProperty(meta.IdentifierPropertyName, meta.IdentifierType, false, true, false);
                dataList.Insert(0, dmap);
            }
            else if (meta.IdentifierType != null && meta.IdentifierType.IsComponentType)
            {
                // composite key is a ComponentType
                var compType = (ComponentType)meta.IdentifierType;

                // check that the component belongs to this class, not a superclass
                if (compType.ReturnedClass == type || meta.IdentifierPropertyName == null || !inheritedProperties.Contains(meta.IdentifierPropertyName))
                {
                    var compNames = compType.PropertyNames;
                    for (int i = 0; i < compNames.Length; i++)
                    {
                        var compName = compNames[i];

                        var propType = compType.Subtypes[i];
                        if (!propType.IsAssociationType)
                        {
                            var dmap = MakeDataProperty(compName, propType, compType.PropertyNullability[i], true, false);
                            dataList.Insert(0, dmap);
                        }
                        else
                        {
                            var assProp = MakeAssociationProperty(persister, (IAssociationType)propType, compName, dataList, true);
                            navList.Add(assProp);
                        }
                    }
                }
            }

            // We do the association properties after the data properties, so we can do the foreign key lookups
            for (int i = 0; i < propNames.Length; i++)
            {
                var propName = propNames[i];
                if (inheritedProperties.Contains(propName)) continue;  // skip property defined on superclass 

                var propType = propTypes[i];
                if (propType.IsAssociationType)
                {
                    // navigation property
                    var assProp = MakeAssociationProperty(persister, (IAssociationType)propType, propName, dataList, false);
                    navList.Add(assProp);
                }
            }
        }

        /// <summary>
        /// Return names of all properties that are defined in the mapped ancestors of the 
        /// given persister.  Note that unmapped superclasses are deliberately ignored, because
        /// they shouldn't affect the metadata.
        /// </summary>
        /// <param name="persister"></param>
        /// <returns>set of property names.  Empty if the persister doesn't have a superclass.</returns>
        HashSet<string> GetSuperProperties(AbstractEntityPersister persister)
        {
            HashSet<string> set = new HashSet<String>();
            if (!persister.IsInherited) return set;
            string superClassName = persister.MappedSuperclass;
            if (superClassName == null) return set;

            IClassMetadata superMeta = _sessionFactory.GetClassMetadata(superClassName);
            if (superMeta == null) return set;

            string[] superProps = superMeta.PropertyNames;
            set = new HashSet<string>(superProps);
            set.Add(superMeta.IdentifierPropertyName);
            return set;
        }


        /// <summary>
        /// Adds a complex type definition
        /// </summary>
        /// <param name="compType">The complex type</param>
        /// <param name="columnNames">The names of the columns which the complex type spans.</param>
        /// <returns>The class name and namespace of the complex type, in the form "Location:#Breeze.Nhibernate.NorthwindIBModel"</returns>
        string AddComponent(ComponentType compType, string[] columnNames)
        {
            var type = compType.ReturnedClass;

            // "Location:#Breeze.Nhibernate.NorthwindIBModel"
            var classKey = type.Name + ":#" + type.Namespace;
            if (_typeNames.Contains(classKey))
            {
                // Only add a complex type definition once.
                return classKey;
            }

            var cmap = new Dictionary<string, object>();
            _typeList.Insert(0, cmap);
            _typeNames.Add(classKey);

            cmap.Add("shortName", type.Name);
            cmap.Add("namespace", type.Namespace);
            cmap.Add("isComplexType", true);

            var dataList = new List<Dictionary<string, object>>();
            cmap.Add("dataProperties", dataList);

            var propNames = compType.PropertyNames;
            var propTypes = compType.Subtypes;
            var propNull = compType.PropertyNullability;

            var colIndex = 0;
            for (int i = 0; i < propNames.Length; i++)
            {
                var propType = propTypes[i];
                var propName = propNames[i];
                if (propType.IsComponentType)
                {
                    // nested complex type
                    var compType2 = (ComponentType)propType;
                    var span = compType2.GetColumnSpan((IMapping)_sessionFactory);
                    var subColumns = columnNames.Skip(colIndex).Take(span).ToArray();
                    var complexTypeName = AddComponent(compType2, subColumns);
                    var compMap = new Dictionary<string, object>();
                    compMap.Add("nameOnServer", propName);
                    compMap.Add("complexTypeName", complexTypeName);
                    compMap.Add("isNullable", propNull[i]);
                    dataList.Add(compMap);
                    colIndex += span;
                }
                else
                {
                    // data property
                    var dmap = MakeDataProperty(propName, propType, propNull[i], false, false);
                    dataList.Add(dmap);
                    colIndex++;
                }
            }
            return classKey;
        }

        /// <summary>
        /// Make data property metadata for the entity
        /// </summary>
        /// <param name="propName">name of the property on the server</param>
        /// <param name="type">data type of the property, e.g. Int32</param>
        /// <param name="isNullable">whether the property is nullable in the database</param>
        /// <param name="isKey">true if this property is part of the key for the entity</param>
        /// <param name="isVersion">true if this property contains the version of the entity (for a concurrency strategy)</param>
        /// <returns></returns>
        private Dictionary<string, object> MakeDataProperty(string propName, IType type, bool isNullable, bool isKey, bool isVersion)
        {
            string newType;
            var typeName = (BreezeTypeMap.TryGetValue(type.Name, out newType)) ? newType : type.Name;

            var dmap = new Dictionary<string, object>();
            dmap.Add("nameOnServer", propName);
            dmap.Add("dataType", typeName);
            dmap.Add("isNullable", isNullable);

            var sqlTypes = type.SqlTypes((ISessionFactoryImplementor)this._sessionFactory);
            var sqlType = sqlTypes[0];

            // This doesn't work; NH does not pick up the default values from the property/column definition
            //if (type is PrimitiveType && !(type is DateTimeOffsetType))
            //{ 
            //    var def = ((PrimitiveType)type).DefaultValue;
            //    if (def != null && def.ToString() != "0")
            //        dmap.Add("defaultValue", def);
            //}

            if (isKey)
            {
                dmap.Add("isPartOfKey", true);
            }
            if (isVersion)
            {
                dmap.Add("concurrencyMode", "Fixed");
            }

            var validators = new List<Dictionary<string, object>>();

            if (!isNullable)
            {
                validators.Add(new Dictionary<string, object>() {
                    {"name", "required" },
                });
            }
            if (sqlType.LengthDefined)
            {
                dmap.Add("maxLength", sqlType.Length);

                validators.Add(new Dictionary<string, object>() {
                    {"maxLength", sqlType.Length },
                    {"name", "maxLength" }
                });
            }

            string validationType;
            if (ValidationTypeMap.TryGetValue(typeName, out validationType))
            {
                validators.Add(new Dictionary<string, object>() {
                    {"name", validationType },
                });
            }

            if (validators.Any())
                dmap.Add("validators", validators);

            return dmap;
        }


        /// <summary>
        /// Make association property metadata for the entity.
        /// Also populates the ForeignKeyMap which is used for related-entity fixup in NHContext.FixupRelationships
        /// </summary>
        /// <param name="containingPersister">Entity Persister containing the property</param>
        /// <param name="propType">Association property</param>
        /// <param name="propName">Name of the property</param>
        /// <param name="dataProperties">Data properties already collected for the containingType.  "isPartOfKey" may be added to a property.</param>
        /// <param name="isKey">Whether the property is part of the key</param>
        /// <returns></returns>
        private Dictionary<string, object> MakeAssociationProperty(AbstractEntityPersister containingPersister, IAssociationType propType, string propName, List<Dictionary<string, object>> dataProperties, bool isKey)
        {
            var nmap = new Dictionary<string, object>();
            nmap.Add("nameOnServer", propName);

            var relatedEntityType = GetEntityType(propType.ReturnedClass, propType.IsCollectionType);
            nmap.Add("entityTypeName", relatedEntityType.Name + ":#" + relatedEntityType.Namespace);
            nmap.Add("isScalar", !propType.IsCollectionType);

            // the associationName must be the same at both ends of the association.
            Type containingType = containingPersister.EntityMetamodel.Type;
            string[] columnNames = GetPropertyColumnNames(containingPersister, propName, propType);
            nmap.Add("associationName", GetAssociationName(containingType.Name, relatedEntityType.Name, columnNames));

            string[] fkNames = null;
            var joinable = propType.GetAssociatedJoinable((ISessionFactoryImplementor)this._sessionFactory);
            if (propType.IsCollectionType)
            {
                // inverse foreign key
                var collectionPersister = joinable as AbstractCollectionPersister;
                if (collectionPersister != null)
                {
                    // many-to-many relationships do not have a direct connection on the client or in metadata
                    var elementPersister = collectionPersister.ElementPersister as AbstractEntityPersister;
                    if (elementPersister != null)
                    {
                        fkNames = GetPropertyNamesForColumns(elementPersister, columnNames);
                        if (fkNames != null)
                            nmap.Add("invForeignKeyNamesOnServer", fkNames);
                    }
                }
            }
            else
            {
                // Not a collection type - a many-to-one or one-to-one association
                var entityRelationship = containingType.FullName + '.' + propName;
                // Look up the related foreign key name using the column name
                fkNames = GetPropertyNamesForColumns(containingPersister, columnNames);
                if (fkNames != null)
                {
                    if (propType.ForeignKeyDirection == ForeignKeyDirection.ForeignKeyFromParent)
                    {
                        nmap.Add("foreignKeyNamesOnServer", fkNames);
                    }
                    else
                    {
                        nmap.Add("invForeignKeyNamesOnServer", fkNames);
                    }

                    // For many-to-one and one-to-one associations, save the relationship in ForeignKeyMap for re-establishing relationships during save
                    _map.ForeignKeyMap.Add(entityRelationship, string.Join(",", fkNames));
                    if (isKey)
                    {
                        foreach(var fkName in fkNames)
                        {
                            var relatedDataProperty = FindPropertyByName(dataProperties, fkName);
                            if (!relatedDataProperty.ContainsKey("isPartOfKey"))
                            {
                                relatedDataProperty.Add("isPartOfKey", true);
                            }
                        }
                    }
                }
                else if (fkNames == null)
                {
                    nmap.Add("foreignKeyNamesOnServer", columnNames);
                    nmap.Add("ERROR", "Could not find matching fk for property " + entityRelationship);
                    _map.ForeignKeyMap.Add(entityRelationship, string.Join(",", columnNames));
                    throw new ArgumentException("Could not find matching fk for property " + entityRelationship);
                }
            }

            return nmap;
        }

        /// <summary>
        /// Get the column names for a given property as a comma-delimited string of unbracketed names.
        /// For a collection property, the column name is the inverse foreign key (i.e. the column on 
        /// the other table that points back to the persister's table)
        /// </summary>
        /// <param name="persister"></param>
        /// <param name="propertyName"></param>
        /// <param name="propType"></param>
        /// <returns></returns>
        string[] GetPropertyColumnNames(AbstractEntityPersister persister, string propertyName, IType propType)
        {
            string[] propColumnNames = null;
            if (propType.IsCollectionType)
            {
                propColumnNames = ((CollectionType)propType).GetReferencedColumns((ISessionFactoryImplementor)this._sessionFactory);
            }
            else
            {
                propColumnNames = persister.GetPropertyColumnNames(propertyName);
            }
            if (propColumnNames == null || propColumnNames.Length == 0)
            {
                // this happens when the property is part of the key
                propColumnNames = persister.KeyColumnNames;
            }
            // HACK for NHibernate formula: when using formula propColumnNames[0] equals null
            if (propColumnNames[0] == null)
            {
                propColumnNames = new string[] { propertyName };
            }
            return UnBracket(propColumnNames);
        }


        /// <summary>
        /// Gets the properties matching the given columns.  May be a component, but will not be an association.
        /// </summary>
        /// <param name="persister"></param>
        /// <param name="columnNames">Array of column names</param>
        /// <returns></returns>
        static string[] GetPropertyNamesForColumns(AbstractEntityPersister persister, string[] columnNames)
        {
            var propNames = persister.PropertyNames;
            var propTypes = persister.PropertyTypes;
            for (int i = 0; i < propNames.Length; i++)
            {
                var propName = propNames[i];
                var propType = propTypes[i];
                if (propType.IsAssociationType) continue;
                var columnArray = persister.GetPropertyColumnNames(i);
                // HACK for NHibernate formula: when using formula GetPropertyColumnNames(i) returns an array with the first element null
                if (columnArray[0] == null)
                {
                    continue;
                }
                if (NamesEqual(columnArray, columnNames)) return new string[] { propName };
            }

            // If we got here, maybe the property is the identifier
            var keyColumnArray = persister.KeyColumnNames;
            if (NamesEqual(keyColumnArray, columnNames))
            {
                if (persister.IdentifierPropertyName != null)
                {
                    return new string[] { persister.IdentifierPropertyName };
                }
                if (persister.IdentifierType.IsComponentType)
                {
                    var compType = (ComponentType)persister.IdentifierType;
                    return compType.PropertyNames;
                }
            }

            if (columnNames.Length > 1)
            {
                // go one-by-one through columnNames, trying to find a matching property.
                // TODO: maybe this should split columnNames into all possible combinations of ordered subsets, and try those
                var propList = new List<string>();
                var prop = new string[1];
                for (int i = 0; i < columnNames.Length; i++)
                {
                    prop[0] = columnNames[i];
                    var names = GetPropertyNamesForColumns(persister, prop);  // recursive call
                    if (names != null) propList.AddRange(names);
                }
                if (propList.Count > 0) return propList.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Unbrackets the column names and concatenates them into a comma-delimited string
        /// </summary>
        static string CatColumnNames(string[] columnNames, char delim = ',')
        {
            var sb = new StringBuilder();
            foreach (var s in columnNames)
            {
                if (sb.Length > 0) sb.Append(delim);
                sb.Append(UnBracket(s));
            }
            return sb.ToString();
        }

        /// <summary>
        /// return true if the two arrays contain the same names, false otherwise.
        /// Names are compared after UnBracket(), and are case-insensitive.
        /// </summary>
        static bool NamesEqual(string[] a, string[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i=0; i<a.Length; i++)
            {
                if (UnBracket(a[i]).ToLower() != UnBracket(b[i]).ToLower()) return false;
            }
            return true;
        }

        /// <summary>
        /// Get the column name without square brackets or quotes around it.  E.g. "[OrderID]" -> OrderID
        /// Because sometimes Hibernate gives us brackets, and sometimes it doesn't.
        /// Double-quotes happen with SQL CE.  Backticks happen with MySQL.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static string UnBracket(string name)
        {
            name = (name[0] == '[') ? name.Substring(1, name.Length - 2) : name;
            name = (name[0] == '"') ? name.Substring(1, name.Length - 2) : name;
            name = (name[0] == '`') ? name.Substring(1, name.Length - 2) : name;
            return name;
        }

        /// <summary>
        /// Return a new array containing the UnBracketed names
        /// </summary>
        static string[] UnBracket(string[] names)
        {
            var u = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                u[i] = UnBracket(names[i]);
            }
            return u;
        }

        /// <summary>
        /// Find the property in the list that has the given name.
        /// </summary>
        /// <param name="properties">list of DataProperty or NavigationProperty maps</param>
        /// <param name="name">matched against the nameOnServer value of entries in the list</param>
        /// <returns></returns>
        static Dictionary<string, object> FindPropertyByName(List<Dictionary<string, object>> properties, string name)
        {
            object nameOnServer;
            foreach(var prop in properties) 
            {
                if (prop.TryGetValue("nameOnServer", out nameOnServer))
                {
                    if (((string) nameOnServer) == name) return prop;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the Breeze name of the entity type.
        /// For collections, Breeze expects the name of the element type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isCollectionType"></param>
        /// <returns></returns>
        static Type GetEntityType(Type type, bool isCollectionType)
        {
            if (!isCollectionType)
            {
                return type;
            }
            else if (type.HasElementType)
            {
                return type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                return type.GetGenericArguments()[0];
            }
            throw new ArgumentException("Don't know how to handle " + type);
        }

        /// <summary>
        /// lame pluralizer.  Assumes we just need to add a suffix.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static string Pluralize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var last = s.Length - 1;
            var c = s[last];
            switch (c)
            {
                case 'y':
                    return s.Substring(0, last) + "ies";
                default:
                    return s + 's';
            }
        }

        /// <summary>
        /// Creates an association name from two entity names.
        /// For consistency, puts the entity names in alphabetical order.
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        /// <param name="propType">Used to ensure the association name is unique for a type</param>
        /// <returns></returns>
        static string GetAssociationName(string name1, string name2, string[] columnNames)
        {
            var cols = CatColumnNames(columnNames, '_');
            if (name1.CompareTo(name2) < 0)
                return ASSN + name1 + '_' + name2 + '_' + cols;
            else
                return ASSN + name2 + '_' + name1 + '_' + cols;
        }
        const string ASSN = "AN_";

        // Map of NH datatype to Breeze datatype.
        static Dictionary<string, string> BreezeTypeMap = new Dictionary<string, string>() {
                    {"Byte[]", "Binary" },
                    {"BinaryBlob", "Binary" },
                    {"Timestamp", "DateTime" },
                    {"TimeAsTimeSpan", "Time" }
                };


        // Map of data type to Breeze validation type
        static Dictionary<string, string> ValidationTypeMap = new Dictionary<string, string>() {
                    {"Boolean", "bool" },
                    {"Byte", "byte" },
                    {"DateTime", "date" },
                    {"DateTimeOffset", "date" },
                    {"Decimal", "number" },
                    {"Guid", "guid" },
                    {"Int16", "int16" },
                    {"Int32", "int32" },
                    {"Int64", "integer" },
                    {"Single", "number" },
                    {"Time", "duration" },
                    {"TimeAsTimeSpan", "duration" }
                };


    }

    /// <summary>
    /// Metadata describing the entity model.  Converted to JSON to send to Breeze client.
    /// </summary>
    public class Metadata : Dictionary<string, object>
    {
        /// <summary>
        /// Map of relationship name -> foreign key name, e.g. "Customer" -> "CustomerID".
        /// Used for re-establishing the entity relationships from the foreign key values during save.
        /// This part is not sent to the client because it is separate from the base dictionary implementation.
        /// </summary>
        public IDictionary<string, string> ForeignKeyMap;
    }
}
