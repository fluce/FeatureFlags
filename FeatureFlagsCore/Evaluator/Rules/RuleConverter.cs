using System;
using System.Reflection;
using FeatureFlags.Rules;
using Newtonsoft.Json.Linq;

namespace FeatureFlags
{
    public class RuleConverter : JsonCreationConverter<Rule>
    {
        protected override Rule Create(Type objectType, JObject jObject)
        {
            var name = jObject["rule"].Value<string>();

            return (Rule)Activator.CreateInstance(Assembly.GetEntryAssembly()
                .GetType($"FeatureFlags.Rules.{name}Rule", true, false));
        }

        private bool FieldExists(string fieldName, JObject jObject)
        {
            return jObject[fieldName] != null;
        }
    }
}