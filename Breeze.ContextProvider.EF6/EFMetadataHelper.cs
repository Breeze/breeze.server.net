using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Breeze.ContextProvider.EF6
{
    public class EFMetadataHelper
    {
        public virtual string BuildJsonMetadata(Object context, string altMetadata)
        {
            var json = GetMetadataFromContext(context);
            altMetadata = altMetadata ?? BuildAltJsonMetadata(context);
            if (altMetadata != null)
            {
                json = "{ \"altMetadata\": " + altMetadata + "," + json.Substring(1);
            }
            return json;
        }

        protected virtual string BuildAltJsonMetadata(Object context)
        {
            // default implementation
            return null; // "{ \"foo\": 8, \"bar\": \"xxx\" }";
        }
        protected virtual String GetConnectionStringFromConfig(String connectionName)
        {
            var item = ConfigurationManager.ConnectionStrings[connectionName];
            return item.ConnectionString;
        }

        public virtual String GetMetadataFromDbFirstAssembly(Assembly assembly, String resourcePrefix = "")
        {
            var xDoc = GetXdocMetadataFromDbFirstAssembly(assembly, resourcePrefix);
            return XDocToJson(xDoc);
        }
        public virtual XDocument GetXdocMetadataFromDbFirstAssembly(Assembly assembly, String resourcePrefix = "")
        {
            var xDoc = GetEmbeddedXDoc(assembly, resourcePrefix + ".csdl");
            // This is needed because the raw edmx has a different namespace than the CLR types that it references.
            xDoc = UpdateCSpaceOSpaceMapping(xDoc, assembly, resourcePrefix);

            return xDoc;
        }
        public virtual String GetMetadataFromContext(Object context)
        {
            var xDoc = GetXdocMetadataFromContext(context);
            return XDocToJson(xDoc);
        }
        public virtual XDocument GetXdocMetadataFromContext(Object context)
        {
            if (context is DbContext)
            {
                return GetXdocMetadataFromDbContext(context);
            }
            return GetXdocMetadataFromObjectContext(context);
        }
        public virtual String XDocToJson(XDocument xDoc)
        {

            var sw = new StringWriter();
            using (var jsonWriter = new JsonPropertyFixupWriter(sw))
            {
                // jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                var jsonSerializer = new JsonSerializer();
                var converter = new XmlNodeConverter();
                jsonSerializer.Converters.Add(converter);
                jsonSerializer.Serialize(jsonWriter, xDoc);
            }

            var jsonText = sw.ToString();
            return jsonText;
        }

        private XDocument GetXdocMetadataFromDbContext(Object context)
        {
            var dbContext = (DbContext)context;
            XElement xele;

            try
            {
                using (var swriter = new StringWriter())
                {
                    using (var xwriter = new XmlTextWriter(swriter))
                    {
                        EdmxWriter.WriteEdmx(dbContext, xwriter);
                        xele = XElement.Parse(swriter.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                if (e is NotSupportedException)
                {
                    // DbContext that fails on WriteEdmx is likely a DataBase first DbContext.
                    return GetXdocMetadataFromObjectContext(dbContext);
                }
                throw;
            }

            var ns = xele.Name.Namespace;
            var conceptualEle = xele.Descendants(ns + "ConceptualModels").First();
            var schemaEle = conceptualEle.Elements().First(ele => ele.Name.LocalName == "Schema");
            var xDoc = XDocument.Load(schemaEle.CreateReader());

            var objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            // This is needed because the raw edmx has a different namespace than the CLR types that it references.
            xDoc = UpdateCSpaceOSpaceMapping(xDoc, objectContext);
            return xDoc;
        }

        private XDocument GetXdocMetadataFromObjectContext(Object context)
        {

            var ocAssembly = context.GetType().Assembly;
            //var ocNamespace = context.GetType().Namespace; // not used

            var objectContext = GetObjectContext(context);
            var normalizedResourceName = ExtractResourceName(objectContext);
            var xDoc = GetEmbeddedXDoc(ocAssembly, normalizedResourceName);

            // This is needed because the raw edmx has a different namespace than the CLR types that it references.
            xDoc = UpdateCSpaceOSpaceMapping(xDoc, objectContext);
            return xDoc;
        }

        private ObjectContext GetObjectContext(Object context)
        {
            ObjectContext objectContext;
            if (context is DbContext)
            {
                var dbContext = (DbContext)context;
                objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            }
            else
            {
                objectContext = (ObjectContext)context;
            }
            return objectContext;
        }

        private string ExtractResourceName(ObjectContext objectContext)
        {
            var ec = objectContext.Connection as EntityConnection;

            if (ec == null)
            {
                throw new Exception("Unable to create an EntityConnection for this ObjectContext");
            }
            var ecBuilder = new EntityConnectionStringBuilder(ec.ConnectionString);
            string metadataString;
            if (!String.IsNullOrEmpty(ecBuilder.Name))
            {
                metadataString = GetConnectionStringFromConfig(ecBuilder.Name);
            }
            else if (!String.IsNullOrEmpty(ecBuilder.Metadata))
            {
                metadataString = ecBuilder.Metadata;
            }
            else
            {
                throw new Exception("Unable to locate EDMX metadata for " + ec.ConnectionString);
            }

            var csdlResource = metadataString.Split('|', ';', '=')
              .FirstOrDefault(s =>
              {
                  s = s.Trim();
                  return s.StartsWith(ResourcePrefix) && s.EndsWith(".csdl");
              });
            if (csdlResource == null)
            {
                throw new Exception("Unable to locate a 'csdl' resource within this connection:" + ec.ConnectionString);
            }

            var parts = csdlResource.Split('/', '.');
            var normalizedResourceName = String.Join(".", parts.Skip(parts.Length - 2));
            return normalizedResourceName;
        }

        private XDocument GetEmbeddedXDoc(Assembly ocAssembly, String resourceSuffix)
        {

            var resourceNames = ocAssembly.GetManifestResourceNames();
            var manifestResourceName = resourceNames.FirstOrDefault(n => n.EndsWith(resourceSuffix));

            if (manifestResourceName == null)
            {
                manifestResourceName = resourceNames.FirstOrDefault(n =>
                  n == "System.Data.Entity.Core.Resources.DbProviderServices.ConceptualSchemaDefinition.csdl"
                );
                if (manifestResourceName == null)
                {
                    throw new Exception("Unable to locate an embedded resource with the name " +
                                        "'System.Data.Entity.Core.Resources.DbProviderServices.ConceptualSchemaDefinition.csdl'" +
                                        " or a resource that ends with: " + resourceSuffix);
                }
            }
            XDocument xDoc;
            using (var mmxStream = ocAssembly.GetManifestResourceStream(manifestResourceName))
            {
                xDoc = XDocument.Load(mmxStream);
            }

            return xDoc;
        }

        private XDocument UpdateCSpaceOSpaceMapping(XDocument xDoc, ObjectContext oc)
        {
            var metadataWs = oc.MetadataWorkspace;

            // ForceOSpaceLoad
            var asm = oc.GetType().Assembly;
            metadataWs.LoadFromAssembly(asm);

            return UpdateCSpaceOSpaceMappingCore(xDoc, metadataWs);

        }

        private XDocument UpdateCSpaceOSpaceMapping(XDocument xDoc, Assembly assembly, String resourcePrefix)
        {
            String[] res;
            if (resourcePrefix == "")
            {
                res = new[] { "res://*/" };
            }
            else
            {
                var pre = "res://*/" + resourcePrefix;
                res = new[] { pre + ".csdl", pre + ".msl", pre + ".ssdl" };
            }
            var metadataWs = new MetadataWorkspace(
              res,
              new[] { assembly });

            // force an OSpace load - UGH - this was hard to find.... need to create the object item collection before loading assembly
            // Method is declared obsolete but it appears that we need it.
#pragma warning disable 618
            metadataWs.RegisterItemCollection(new ObjectItemCollection());
#pragma warning restore 618
            metadataWs.LoadFromAssembly(assembly);

            return UpdateCSpaceOSpaceMappingCore(xDoc, metadataWs);
        }

        private XDocument UpdateCSpaceOSpaceMappingCore(XDocument xDoc, MetadataWorkspace metadataWs)
        {
            var cspaceTypes = metadataWs.GetItems<StructuralType>(DataSpace.CSpace);
            var tpls = cspaceTypes
                .Where(st => !(st is AssociationType))
                .Select(st =>
                {
                    var ost = metadataWs.GetObjectSpaceType(st);
                    return new[] { st.FullName, ost.FullName };
                })
                .ToList();
            var ocMapping = JsonConvert.SerializeObject(tpls);
            Debug.Assert(xDoc.Root != null, "xDoc.Root != null");
            xDoc.Root.SetAttributeValue("CSpaceOSpaceMapping", ocMapping);
            return xDoc;
        }

        private const string ResourcePrefix = @"res://";

        /* JsonPropertyFixupWriter
         * Copied from the original in ContextProvider because needed in this helper 
         * which must be independent of any ContextProvider.
         * Buried within the helper class so as not to conflict with ContextProvider's.
         */
        public class JsonPropertyFixupWriter : JsonTextWriter
        {
            public JsonPropertyFixupWriter(TextWriter textWriter)
                : base(textWriter)
            {
                _isDataType = false;
            }

            public override void WritePropertyName(string name)
            {
                if (name.StartsWith("@"))
                {
                    name = name.Substring(1);
                }
                name = ToCamelCase(name);
                _isDataType = name == "type";
                base.WritePropertyName(name);
            }

            public override void WriteValue(string value)
            {
                if (_isDataType && !value.StartsWith("Edm."))
                {
                    base.WriteValue("Edm." + value);
                }
                else
                {
                    base.WriteValue(value);
                }
            }

            private static string ToCamelCase(string s)
            {
                if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
                {
                    return s;
                }
                string str = char.ToLower(s[0], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                if (s.Length > 1)
                {
                    str = str + s.Substring(1);
                }
                return str;
            }

            private bool _isDataType;
        }
    }
}
