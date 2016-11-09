namespace FeatureFlags.Evaluator.Rules
{
    public class InactiveRule : Rule
    {
        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return FeatureFlagState.Inactive;
        }
    }
}