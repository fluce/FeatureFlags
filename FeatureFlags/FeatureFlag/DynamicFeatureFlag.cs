using FeatureFlags.Stores;

namespace FeatureFlags.FeatureFlag
{
    public class DynamicFeatureFlag : IFeatureFlag
    {
        public string Name { get; private set; }

        public IDynamicFeatureStore DynamicFeatureStore { get; set; }

        public DynamicFeatureFlag(string name, IDynamicFeatureStore dynamicFeatureStore)
        {
            Name = name;
            DynamicFeatureStore = dynamicFeatureStore;
        }

        public FeatureFlagState GetState(FeatureContext featureContext)
        {
            return DynamicFeatureStore.GetFeatureState(Name, featureContext);
        }


    }
}