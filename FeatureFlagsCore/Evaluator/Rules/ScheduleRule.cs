using System;
using Newtonsoft.Json;

namespace FeatureFlags.Rules
{
    public class ScheduleRule : Rule
    {
        [JsonProperty("from")]
        public DateTime From { get; set; }

        [JsonProperty("to")]
        public DateTime To { get; set; }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return context.DateTime >= From && context.DateTime < To
                ? FeatureFlagState.Active
                : FeatureFlagState.Inactive;
        }

    }
}