using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;

namespace MturkSms.Api
{
    public class SnakeCaseContractResolver : DefaultContractResolver
    {
        public SnakeCaseContractResolver() : base()
        {
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            return Regex.Replace(propertyName, @"(\w)([A-Z])", "$1_$2").ToLower();
        }
    }
}
