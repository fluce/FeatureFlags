using System;
using System.Reflection;
using FeatureFlags.Utils;
using Newtonsoft.Json.Linq;

namespace FeatureFlags.Evaluator.Rules
{
    public class RuleConverter : JsonCreationConverter<Rule>
    {
        protected override Rule Create(Type objectType, JObject jObject)
        {
            var name = jObject["rule"].Value<string>();

            return (Rule)Activator.CreateInstance(Assembly.GetCallingAssembly()
                .GetType($"FeatureFlags.Evaluator.Rules.{name}Rule", true, false));
        }

        private bool FieldExists(string fieldName, JObject jObject)
        {
            return jObject[fieldName] != null;
        }
    }
}