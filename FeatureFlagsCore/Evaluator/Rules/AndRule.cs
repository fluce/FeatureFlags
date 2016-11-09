using System.Linq;
using Newtonsoft.Json;

namespace FeatureFlags.Rules
{
    public class AndRule : Rule
    {
        [JsonProperty("rules")]
        public Rule[] Rules { get; set; }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return Rules.All(x => x.Evaluate(context) == FeatureFlagState.Active)
                ? FeatureFlagState.Active
                : FeatureFlagState.Inactive;
        }
    }
}