using Newtonsoft.Json;
using NHibernate;
using NHibernate.Proxy;
using System;

namespace Breeze.ContextProvider.NH
{
    /// <summary>
    /// JsonConverter for handling NHibernate proxies.  
    /// Only serializes the object if it is initialized, i.e. the proxied object has been loaded.
    /// </summary>
    /// <see cref="http://james.newtonking.com/projects/json/help/html/T_Newtonsoft_Json_JsonConverter.htm"/>
    public class NHibernateProxyJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (NHibernateUtil.IsInitialized(value))
            {
                var proxy = value as INHibernateProxy;
                if (proxy != null)
                {
                    value = proxy.HibernateLazyInitializer.GetImplementation();
                }

                var resolver = serializer.ReferenceResolver;
                if (resolver.IsReferenced(serializer, value))
                {
                    // we've already written the object once; this time, just write the reference
                    // We have to do this manually because we have our own JsonConverter.
                    var valueRef = resolver.GetReference(serializer, value);
                    writer.WriteStartObject();
                    writer.WritePropertyName("$ref");
                    writer.WriteValue(valueRef);
                    writer.WriteEndObject();
                }
                else
                {
                    serializer.Serialize(writer, value);
                }
            }
            else
            {
                serializer.Serialize(writer, null);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(INHibernateProxy).IsAssignableFrom(objectType);
        }
    }
}
