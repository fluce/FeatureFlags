namespace FeatureFlags
{
    public abstract class FeatureFlagStateEvaluator
    {
        public abstract FeatureFlagState Evaluate(FeatureContext context);
    }
}