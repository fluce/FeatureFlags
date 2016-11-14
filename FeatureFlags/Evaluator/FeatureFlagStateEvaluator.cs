namespace FeatureFlags.Evaluator
{
    public abstract class FeatureFlagStateEvaluator
    {
        public string Key { get; set; }
        public abstract FeatureFlagState Evaluate(FeatureContext context);
    }
}