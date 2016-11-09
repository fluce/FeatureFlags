using System.Linq;
using Newtonsoft.Json;

namespace FeatureFlags.Evaluator.Rules
{
    public class OrRule : Rule
    {
        [JsonProperty("rules")]
        public Rule[] Rules { get; set; }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return Rules.Any(x => x.Evaluate(context) == FeatureFlagState.Active)
                ? FeatureFlagState.Active
                : FeatureFlagState.Inactive;
        }
    }
}