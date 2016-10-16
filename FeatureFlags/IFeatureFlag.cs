namespace FeatureFlags
{
    public interface IFeatureFlag
    {
        string Name { get; }
        FeatureFlagState GetState(FeatureContext featureContext);
    }
}