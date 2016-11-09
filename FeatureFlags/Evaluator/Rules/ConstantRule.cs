namespace FeatureFlags.Evaluator.Rules
{
    public class ConstantRule : Rule
    {
        public FeatureFlagState Value { get; set; }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return Value;
        }
    }
}