using System.Collections.Concurrent;

namespace FeatureFlags
{
    public class CachingFeatureStore: IFeatureStore
    {
        readonly ConcurrentDictionary<string, IFeatureFlag> features = new ConcurrentDictionary<string, IFeatureFlag>();

        readonly IFeatureStore featureStore;

        public CachingFeatureStore(IFeatureStore aFeatureStore)
        {
            featureStore = aFeatureStore;
        }

        public IFeatureFlag GetFeature(string featureKey)
        {
            return features.GetOrAdd(featureKey, featureStore.GetFeature);
        }
    }
}