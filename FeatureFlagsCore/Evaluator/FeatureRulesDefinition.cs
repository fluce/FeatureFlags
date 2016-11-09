using FeatureFlags.Rules;
using Newtonsoft.Json;

namespace FeatureFlags
{
    public class FeatureRulesDefinition
    {
        [JsonProperty("contextualRules")]
        public Rule[] ContextualRules { get; set; }
    }
}