namespace FeatureFlags.Evaluator
{
    public class ConstantFeatureFlagStateEvaluator : FeatureFlagStateEvaluator
    {
        private FeatureFlagState State { get; }

        public ConstantFeatureFlagStateEvaluator(FeatureFlagState state)
        {
            State = state;
        }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return State;
        }
    }
}