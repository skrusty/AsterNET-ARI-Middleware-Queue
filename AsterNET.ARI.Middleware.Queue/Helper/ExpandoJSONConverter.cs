using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AsterNET.ARI.Middleware.Queue.Helper
{
    //class ExpandoJSONConverter : JsonConverter
    //{
    //    private readonly Type[] _types;

    //    public ExpandoJSONConverter(params Type[] types)
    //    {
    //        _types = types;
    //    }

    //    public override bool CanConvert(Type objectType)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        JToken t = JToken.FromObject(value);
            
    //        if (t.Type != JTokenType.Object)
    //        {
    //            var dictItem = 
    //            JObject o = (JObject)t;
    //            o.AddFirst(new JProperty(, t.Value<bool>()));
    //            o.WriteTo(writer);
    //        }
    //        else
    //        {
    //            t.WriteTo(writer);
    //        }
    //    }
    //}
}
