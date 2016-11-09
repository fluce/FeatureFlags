using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FeatureFlags.FeatureFlag;

namespace FeatureFlags.Stores
{
    public class CachingFeatureStore: IFeatureStore
    {
        readonly ConcurrentDictionary<string, IFeatureFlag> features = new ConcurrentDictionary<string, IFeatureFlag>();

        readonly IFeatureStore featureStore;

        public CachingFeatureStore(IFeatureStore aFeatureStore)
        {
            featureStore = aFeatureStore;
        }

        public IEnumerable<IFeatureFlag> GetAllFeatures()
        {
            return featureStore.GetAllFeatures();
        }

        public IFeatureFlag GetFeature(string featureKey)
        {
            return features.GetOrAdd(featureKey, featureStore.GetFeature);
        }
    }
}