using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq;
using Newtonsoft.Json;
using System.Dynamic;
using System.Reflection;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Breeze.Core;
using System.ComponentModel.DataAnnotations;

namespace Breeze.Persistence.EFCore
{
    public static class MetadataBuilder_EFC2
    {
        public static String GetMetadataFromContext(DbContext context)
        {
            return GetMetadataFromDbContext(context);
        }

        private static String GetMetadataFromDbContext(DbContext dbContext)
        {
            var entTypes = dbContext.Model.GetEntityTypes().OrderBy(et => et.Name).ToList();
            var associationList = new List<string>();
            var enums = new List<Type>();
            #region Init
            Func<string, string> className = fullClassName =>
            {
                return fullClassName.Split('.').Last();
            };
            var nameSpace = className(entTypes.First().ClrType.Namespace);
            var entityContainerName = className(dbContext.GetType().FullName);

            var uniqueRoleNames = new Dictionary<ValueTuple<string, string>, ValueTuple<List<ValueTuple<INavigation, string>>, Int32>>();
            Func<INavigation, string> getRoleName = nav =>
            {
                var principalType = className(nav.DeclaringEntityType.Name);
                var dependentType = className(nav.FindInverse()?.DeclaringEntityType?.Name);
                if (principalType != dependentType /*|| principalType != "BusinessEntity"*/)
                    return principalType;
                var key = (principalType, dependentType);
                if (!uniqueRoleNames.ContainsKey(key))
                {
                    var roleName = principalType;
                    uniqueRoleNames.Add(key, (new List<ValueTuple<INavigation, string>> { (nav, roleName) }, 0));
                    return roleName;
                }
                else
                {
                    var curRoleNames = uniqueRoleNames[key];
                    var curRoleName = curRoleNames.Item1.FirstOrDefault(n => n.Item1 == nav);
                    if (curRoleName.Item1 == nav)
                        return curRoleName.Item2;
                    curRoleNames = (curRoleNames.Item1, curRoleNames.Item2 + 1);
                    uniqueRoleNames[key] = curRoleNames;
                    var roleName = principalType + curRoleNames.Item2.ToString();
                    curRoleNames.Item1.Add((nav, roleName));
                    return roleName;
                }
            };

            try
            {
                var metaInit = $@"
            {{
                ""schema"": {{
			        ""xmlns:annotation"": ""http://schemas.microsoft.com/ado/2009/02/edm/annotation"",
                    ""xmlns:p1"": ""http://schemas.microsoft.com/ado/2009/02/edm/annotation"",
                    ""xmlns"": ""http://schemas.microsoft.com/ado/2009/11/edm"",
                    ""namespace"": ""{nameSpace}"",
                    ""alias"": ""Self"",
                    ""p1:UseStrongSpatialTypes"": ""false""
                }}
			}}
			";

                var converter = new ExpandoObjectConverter();
                dynamic meta = JsonConvert.DeserializeObject<ExpandoObject>(metaInit, converter);
                #endregion

                #region cSpaceOSpaceMapping
                string cSpaceOSpaceMapping = "[";
                entTypes.ForEach(et => cSpaceOSpaceMapping += $@"[""{nameSpace}.{className(et.Name)}"",""{et.Name}""],");
                cSpaceOSpaceMapping = cSpaceOSpaceMapping.TrimEnd(',') + "]";
                meta.schema.cSpaceOSpaceMapping = cSpaceOSpaceMapping;
                #endregion

                #region entityContainer
                dynamic entityContainer = new ExpandoObject();
                meta.schema.entityContainer = entityContainer;
                entityContainer.name = entityContainerName;
                var entityContainerDict = entityContainer as IDictionary<string, Object>;
                entityContainerDict.Add("p1:LazyLoadingEnabled", "false");

                #region entitySet
                var entitySet = new List<object>();
                entityContainer.entitySet = entitySet;
                entTypes.ForEach(et =>
                {
                    dynamic entityProp = new ExpandoObject();
                    entityProp.name = className(et.Name);
                    entityProp.entityType = nameSpace + "." + className(et.Name);
                    entitySet.Add(entityProp);
                });
                #endregion

                #region associationSet
                var associations = new List<object>();
                meta.schema.association = associations;

                var associationSet = new List<object>();
                entityContainer.associationSet = associationSet;
                entTypes.ForEach(entType =>
                {
                    var navs = entType.GetNavigations().ToList();
                    navs.ForEach(nav =>
                    {
                        var inverse = nav.FindInverse();
                        if (inverse == null)
                            return;
                        var properNav = nav.IsDependentToPrincipal() ? inverse : nav;
                        if (properNav == null)
                            properNav = nav;
                        var properInvNav = properNav.FindInverse();
                        if (properInvNav == null)
                            properInvNav = properNav;
                        var associationName = "FK_" + className(properInvNav.DeclaringEntityType.Name) + "_" + properInvNav.Name;
                        dynamic navProp = new ExpandoObject();
                        dynamic assProp = new ExpandoObject();
                        associationSet.Add(navProp);
                        //if (associationName == "FK_EquipmentEvent_User" || associationName == "FK_EquipmentEvent_AddedByUser")
                        //{
                        //    associationName = associationName;
                        //}
                        //if (associationName == "FK_EquipmentEvent_AddedByEquipmentEvent")
                        //{
                        //    associationName = associationName;
                        //}
                        var assAdded = false;
                        if (!associationList.Contains(associationName))
                        {
                            associations.Add(assProp);
                            associationList.Add(associationName);
                            assAdded = true;
                        }
                        navProp.name = associationName;
                        navProp.association = nameSpace + "." + associationName;
                        var end = new List<object>();
                        navProp.end = end;
                        dynamic endProp0 = new ExpandoObject();
                        dynamic endProp1 = new ExpandoObject();
                        end.Add(endProp0);
                        end.Add(endProp1);
                        //endProp0.role = className(properNav.DeclaringEntityType.Name);
                        endProp0.role = getRoleName(properNav);
                        endProp0.entitySet = className(properNav.DeclaringEntityType.Name);
                        //endProp1.role = className(properInvNav.DeclaringEntityType.Name);
                        endProp1.role = getRoleName(properInvNav);
                        endProp1.entitySet = className(properInvNav.DeclaringEntityType.Name);

                        assProp.name = associationName;
                        var assEnds = new List<object>();
                        assProp.end = assEnds;

                        dynamic assEndProp0 = new ExpandoObject();
                        assEnds.Add(assEndProp0);
                        assEndProp0.type = "Edm." + nameSpace + "." + endProp0.entitySet;
                        assEndProp0.role = endProp0.role;
                        assEndProp0.multiplicity = "1";

                        dynamic assEndProp1 = new ExpandoObject();
                        assEnds.Add(assEndProp1);
                        assEndProp1.type = "Edm." + nameSpace + "." + endProp1.entitySet;
                        assEndProp1.role = endProp1.role;
                        assEndProp1.multiplicity = properNav.IsCollection() ? "*" : "1" ;

                        dynamic referentialConstraint = new ExpandoObject();
                        assProp.referentialConstraint = referentialConstraint;
                        dynamic principal = new ExpandoObject();
                        dynamic dependent = new ExpandoObject();
                        principal.role = assEndProp0.role;
                        dependent.role = assEndProp1.role;
                        referentialConstraint.principal = principal;
                        referentialConstraint.dependent = dependent;

                        //if (associationName == "FK_EquipmentEvent_AddedByUser")
                        //{
                        //    associationName = associationName;
                        //}
                        //if (associationName == "FK_CustomerContractXref_Circuit")
                        //{
                        //    associationName = associationName;
                        //}

                        var pkNames = properNav.ForeignKey.PrincipalKey.Properties.Select(p => p.Name);
                        if (pkNames.Count() == 1)
                        {
                            principal.propertyRef = new ExpandoObject();
                            principal.propertyRef.name = pkNames.FirstOrDefault();
                        }
                        else
                        {
                            var propertyRefs = new List<object>();
                            principal.propertyRef = propertyRefs;
                            pkNames.ToList().ForEach(fkName =>
                            {
                                dynamic propertyRef = new ExpandoObject();
                                propertyRef.name = fkName;
                                propertyRefs.Add(propertyRef);
                            });
                        }

                        var isNullableFK = properNav.ForeignKey.Properties.Count(k => k.IsColumnNullable());
                        if (isNullableFK > 0)
                            assEndProp0.multiplicity = "0..1";

                        var fkNames = properNav.ForeignKey.Properties.Select(p => p.Name);
                        if (fkNames.Count() == 1)
                        {
                            dependent.propertyRef = new ExpandoObject();
                            dependent.propertyRef.name = fkNames.FirstOrDefault();
                        }
                        else
                        {
                            var propertyRefs = new List<object>();
                            dependent.propertyRef = propertyRefs;
                            fkNames.ToList().ForEach(fkName =>
                            {
                                dynamic propertyRef = new ExpandoObject();
                                propertyRef.name = fkName;
                                propertyRefs.Add(propertyRef);
                            });
                        }

                    });
                });
                #endregion
                
                #endregion


                #region entityType
                    var entityTypes = new List<object>();
                    meta.schema.entityType = entityTypes;
                    entTypes.ForEach(entType =>
                    {
                        dynamic entityType = new ExpandoObject();
                        entityTypes.Add(entityType);
                        entityType.name = className(entType.Name);
                        dynamic keys = new ExpandoObject();
                        entityType.key = keys;
                        var entKeys = entType.GetProperties().Where(p => p.IsPrimaryKey()).ToList();
                        if (entKeys.Count == 1)
                        {
                            dynamic propertyRef = new ExpandoObject();
                            keys.propertyRef = propertyRef;
                            propertyRef.name = entKeys.First().Name;
                        }
                        else
                        {
                            var propertyRef = new List<object>();
                            keys.propertyRef = propertyRef;
                            entKeys.ForEach(key =>
                            {
                                dynamic keyProp = new ExpandoObject();
                                propertyRef.Add(keyProp);
                                keyProp.name = key.Name;
                            });
                        }

                        var properties = new List<object>();
                        entityType.property = properties;
                        entType.GetProperties().ToList().ForEach(p =>
                        {
                            dynamic fieldProp = new ExpandoObject();
                            properties.Add(fieldProp);
                            fieldProp.name = p.Name;
                            fieldProp.type = NormalizeDataTypeName(p.ClrType);
                            if (fieldProp.type == "Edm.String")
                            {
                                fieldProp.unicode = "true";
                                fieldProp.fixedLength = "false";
                            }
                            if (p.GetMaxLength().HasValue)
                            {
                                if (p.GetMaxLength().Value == Int32.MaxValue)
                                    fieldProp.maxLength = "Max";
                                else
                                    fieldProp.maxLength = p.GetMaxLength().ToString();
                            }
                            if (fieldProp.type == "Edm.Byte")
                            {
                                fieldProp.type = "Edm.Binary";
                                //p.ClrType.IsArray
                                //result == "Edm.Byte[]")
                                var minLengthA = p.GetAnnotations().Where(a => a.Name == "MinLength").FirstOrDefault();
                                if (minLengthA != null)
                                    fieldProp.fixedLength = (int)minLengthA.Value == p.GetMaxLength() ? "true": "false";
                            }

                            if (p.ClrType.IsEnum && !enums.Contains(p.ClrType))
                                enums.Add(p.ClrType);

                            var stringLengthA = p.GetAnnotations().Where(a => a.Name == "StringLength").FirstOrDefault();
                            if (stringLengthA != null) {
                                var stringLen = stringLengthA as StringLengthAttribute;
                                if (stringLen != null)
                                {
                                    if (stringLen.MinimumLength == stringLen.MaximumLength)
                                        fieldProp.fixedLength = "true";
                                    if (!p.GetMaxLength().HasValue)
                                        fieldProp.maxLength = stringLen.MaximumLength.ToString();
                                }
                            }
                            var reqA = p.GetAnnotations().Where(a => a.Name == "Required").FirstOrDefault();
                            if (reqA != null || !p.IsNullable)
                                fieldProp.nullable = "false";
                            if (p.IsPrimaryKey() && p.ValueGenerated == ValueGenerated.OnAdd)
                                (fieldProp as IDictionary<string, object>)["p1:StoreGeneratedPattern"] = "Identity";
                            
                            
                            if (p.ValueGenerated == ValueGenerated.OnAddOrUpdate)
                                (fieldProp as IDictionary<string, object>)["p1:StoreGeneratedPattern"] = "Computed";
                            if (p.IsUnicode().HasValue && p.IsUnicode().Value)
                                fieldProp.unicode = "true";

                            if (entityType.name == "CircuitView" && p.Name == "Id")
                            {
                                Console.WriteLine("");
                            }
                        });

                        var navigationProperties = new List<object>();
                        entityType.navigationProperty = navigationProperties;
                        var navs = entType.GetNavigations().ToList();
                        navs.ForEach(nav =>
                        {
                            var inverse = nav.FindInverse();
                            if (inverse == null)
                                return;
                            var properNav = nav.IsDependentToPrincipal() ? inverse : nav;
                            if (properNav == null)
                                properNav = nav;
                            var properInvNav = properNav.FindInverse();
                            if (properInvNav == null)
                                properInvNav = properNav;
                            
                            var associationName = "FK_" + className(properInvNav.DeclaringEntityType.Name) + "_" + properInvNav.Name;
                            dynamic navProp = new ExpandoObject();
                            navigationProperties.Add(navProp);
                            navProp.name = nav.Name;
                            navProp.relationship = nameSpace + "." + associationName;
                            navProp.fromRole = className(className(nav.DeclaringEntityType.Name));
                            navProp.toRole = className(className(inverse.DeclaringEntityType.Name));
                            navProp.fromRole = getRoleName(nav);
                            navProp.toRole = getRoleName(inverse);
                        });
                    });

                #endregion

                #region enums
                var enumTypes = new List<object>();
                meta.schema.enumType = enumTypes;
                enums.ForEach(enumItem =>
                {
                    dynamic enumType = new ExpandoObject();
                    enumTypes.Add(enumType);
                    enumType.name = className(enumItem.Name);
                    var members = new List<object>();
                    enumType.member = members;
                    foreach (var enumValue in Enum.GetValues(enumItem))
                    {
                        dynamic memberItem = new ExpandoObject();
                        members.Add(memberItem);
                        memberItem.name = enumValue.ToString();
                        //memberItem.value = enumValue.GetHashCode().ToString();
                        memberItem.value = Convert.ToInt32(enumValue).ToString();
                    };
                });
                //
                #endregion

                var ss = new JsonSerializerSettings();
                ss = JsonSerializationFns.UpdateWithDefaults(ss);
                ss.TypeNameHandling = TypeNameHandling.None;
                ss.PreserveReferencesHandling = PreserveReferencesHandling.None;
                var finalJsonIndented = JsonConvert.SerializeObject(meta, Formatting.Indented, ss);
                var finalJson = JsonConvert.SerializeObject(meta, Formatting.None, ss);
                return finalJson;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private static string NormalizeDataTypeName(Type type)
        {
            type = TypeFns.GetNonNullableType(type);
            var result = type.ToString().Replace("System.", "Edm.");
            if (result == "Edm.Byte[]")
            {
                return "Edm.Binary";
            }
            else
            {
                return result;
            }
            return result;
        }
    }

    public static class ExpandoHelpers
    {
        public static void AddProperty(this ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
        public static bool IsValid(this ExpandoObject expando, string propertyName)
        {
            // Check that they supplied a name
            if (string.IsNullOrWhiteSpace(propertyName))
                return false;
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            return expandoDict.ContainsKey(propertyName);
        }
    }

    public class BreezeMetadata
    {
        public string MetadataVersion { get; set; }
        public string NamingConvention { get; set; }
        public List<MetaType> StructuralTypes
        {
            get; set;
        }
    }
}
