using FeatureFlags.Stores.AppSettings;

namespace FeatureFlags
{
    public interface IDynamicFeatureStore: IFeatureStore
    {
        FeatureFlagState GetFeatureState(string name, FeatureContext featureContext);
        FeatureFlagDefinition GetFeatureFlagDefinition(string featureKey);

        void SetFeatureFlagDefinition(FeatureFlagDefinition featureFlagDefinition);
    }
}