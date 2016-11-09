using System;
using Newtonsoft.Json;

namespace FeatureFlags.Evaluator.Rules
{
    [JsonConverter(typeof (RuleConverter))]
    public abstract class Rule
    {
        [JsonProperty("rule")]
        public string RuleType
        {
            get
            {
                var name = GetType().Name;
                if (name.Length > 4 && name.EndsWith("Rule",StringComparison.Ordinal))
                    return name.Substring(0, name.Length - 4);
                return name;
            }
        }

        public abstract FeatureFlagState Evaluate(FeatureContext context);
    }
}