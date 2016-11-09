namespace FeatureFlags.Evaluator
{
    public abstract class FeatureFlagStateEvaluator
    {
        public abstract FeatureFlagState Evaluate(FeatureContext context);
    }
}