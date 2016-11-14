namespace FeatureFlags.Evaluator
{
    public class ConstantFeatureFlagStateEvaluator : FeatureFlagStateEvaluator
    {
        private FeatureFlagState State { get; }

        public ConstantFeatureFlagStateEvaluator(string key, FeatureFlagState state)
        {
            Key = key;
            State = state;
        }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return State;
        }
    }
}