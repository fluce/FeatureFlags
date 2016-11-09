using System.Collections;
using System.Collections.Generic;

namespace FeatureFlags
{
    public interface IFeatureStore
    {
        IFeatureFlag GetFeature(string featureKey);

        IEnumerable<IFeatureFlag> GetAllFeatures();
    }
}