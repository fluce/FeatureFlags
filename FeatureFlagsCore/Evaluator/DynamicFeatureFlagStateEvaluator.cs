using System.Linq;

namespace FeatureFlags
{
    public class DynamicFeatureFlagStateEvaluator : FeatureFlagStateEvaluator
    {
        public FeatureRulesDefinition Rules { get; set; }

        public DynamicFeatureFlagStateEvaluator(FeatureRulesDefinition rules)
        {
            Rules = rules;
        }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return Rules.ContextualRules.All(x => x.Evaluate(context)==FeatureFlagState.Active) 
                ? FeatureFlagState.Active 
                : FeatureFlagState.Inactive;
        }
    }
}