namespace FeatureFlags
{
    public interface IDynamicFeatureStore
    {
        FeatureFlagState GetFeatureState(string name, FeatureContext featureContext);
    }
}