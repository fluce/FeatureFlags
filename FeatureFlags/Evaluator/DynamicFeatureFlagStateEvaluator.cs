using System;
using System.Linq;

namespace FeatureFlags.Evaluator
{
    public class DynamicFeatureFlagStateEvaluator : FeatureFlagStateEvaluator
    {
        public FeatureRulesDefinition Rules { get; set; }

        public DynamicFeatureFlagStateEvaluator(string key, FeatureRulesDefinition rules)
        {
            Key = key;
            Rules = rules;
        }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            if (Rules.OverrideValue.HasValue)
                return Rules.OverrideValue.Value? 
                      FeatureFlagState.Active
                    : FeatureFlagState.Inactive;

            if (Rules.ActiveFunc != null)
            {
                if (context.InternalFeatureContext.RecursiveStack.Contains(Key))
                    throw new InvalidOperationException("Looping recursive call detected");

                try
                {
                    context.InternalFeatureContext.RecursiveStack.Push(Key);
                    var glob = new Globals(context) {Now = context.DateTime};
                    if (context.Email != null || context.Uid != null)
                        glob.User = new Globals.UserInfo {Email = context.Email, Uid = context.Uid};
                    try
                    {
                        return Rules.ActiveFunc(glob)
                            ? FeatureFlagState.Active
                            : FeatureFlagState.Inactive;
                    }
                    catch (Exception)
                    {
                        return FeatureFlagState.Inactive;
                    }
                }
                finally
                {
                    context.InternalFeatureContext.RecursiveStack.Pop();
                }
                
            }

            return Rules.ContextualRules.All(x => x.Evaluate(context)==FeatureFlagState.Active) 
                ? FeatureFlagState.Active 
                : FeatureFlagState.Inactive;
        }
    }
}