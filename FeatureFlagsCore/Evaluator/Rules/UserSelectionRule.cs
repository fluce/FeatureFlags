using System;
using Newtonsoft.Json;

namespace FeatureFlags.Rules
{
    public class UserSelectionRule : Rule
    {
        [JsonProperty("uid")]
        public Guid? Uid { get; set; }

        [JsonProperty("ratio")]
        public decimal? Ratio { get; set; }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            if (Uid.HasValue)
                return Uid == context.Uid
                    ? FeatureFlagState.Active
                    : FeatureFlagState.Inactive;

            if (Ratio.HasValue)
            {
                var guid = context.Uid.ToByteArray();
                int r = guid[14]*256 + guid[15];

                return (r < Ratio*65536)
                    ? FeatureFlagState.Active
                    : FeatureFlagState.Inactive;
            }
            return FeatureFlagState.Inactive;
        }
    }
}