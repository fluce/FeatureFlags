using System;
using FeatureFlags.Evaluator.Rules;
using Newtonsoft.Json;

namespace FeatureFlags.Evaluator
{
    public class FeatureRulesDefinition
    {
        [JsonProperty("override",NullValueHandling = NullValueHandling.Ignore)]
        public bool? OverrideValue { get; set; }
        [JsonProperty("rules", NullValueHandling = NullValueHandling.Ignore)]
        public Rule[] ContextualRules { get; set; }
        [JsonProperty("expr", NullValueHandling = NullValueHandling.Ignore)]
        public string ActiveExpression { get; set; }

        [JsonIgnore]
        public Func<Globals,bool> ActiveFunc { get; set; }
    }

    public class Globals
    {
        private FeatureContext Context { get; set; }

        public Globals(FeatureContext context)
        {
            Context = context;
        }

        public DateTime Now { get; set; }

        public UserInfo User { get; set; }

        public class UserInfo
        {
            public string Email { get; set; }
            public Guid? Uid { get; set; }

            public bool Sample(decimal ratio)
            {
                var guid = Uid.Value.ToByteArray();
                int r = guid[14] * 256 + guid[15];

                return r < ratio*65536;
            }
        }

        public bool IsActive(string featureFlag)
        {
            return Context?.InternalFeatureContext?.Features?.IsActive(featureFlag, Context) ?? false;
        }

    }
}