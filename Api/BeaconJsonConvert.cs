using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MturkSms.Api
{
    public static class BeaconJsonConvert
    {
        public static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new SnakeCaseContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter(true) }
            });
        }

        public static T DeserializeObject<T>(string s)
        {
            return JsonConvert.DeserializeObject<T>(s, new JsonSerializerSettings
            {
                ContractResolver = new SnakeCaseContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter(true) }
            });
        }
    }
}