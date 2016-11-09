using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FeatureFlags
{
    public static class FeatureFlagEvaluatorUtils
    {
        public static FeatureFlagStateEvaluator Parse(string v)
        {
            bool state = false;
            if (v != null && !bool.TryParse(v, out state))
            {
                int stateAsInt;
                if (int.TryParse(v, out stateAsInt))
                    state = stateAsInt == 1;
                else if (string.Compare(v, "on", StringComparison.OrdinalIgnoreCase) == 0)
                    state = true;
                else if (string.Compare(v, "yes", StringComparison.OrdinalIgnoreCase) == 0)
                    state = true;
                else
                {
                    try
                    {
                        var rules=JsonConvert.DeserializeObject<FeatureRulesDefinition>(v);

                        return new DynamicFeatureFlagStateEvaluator(rules);
                    }
                    catch (JsonReaderException)
                    {
                        state = false;
                    }

                }
            }
            return new ConstantFeatureFlagStateEvaluator(state ? FeatureFlagState.Active : FeatureFlagState.Inactive);
        }
    }


}