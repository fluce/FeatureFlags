namespace FeatureFlags.Rules
{
    public class ActiveRule : Rule
    {
        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return FeatureFlagState.Active;
        }
    }
}