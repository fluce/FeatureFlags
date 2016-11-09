namespace FeatureFlags.FeatureFlag
{
    public class StaticFeatureFlag : IFeatureFlag
    {
        public string Name { get; private set; }

        public FeatureFlagState State { get; set; }

        public StaticFeatureFlag(string name, FeatureFlagState state)
        {
            Name = name;
            State = state;
        }

        public FeatureFlagState GetState(FeatureContext featureContext)
        {
            return State;
        }


    }
}