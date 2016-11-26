using System.Collections.Generic;

namespace FeatureFlags
{
    public interface IFeatures
    {
        IFeatureContextProvider ContextProvider { get; }
        bool IsActive(string featureKey, FeatureContext featureContext);
        bool IsActive(string featureKey);

        IEnumerable<IFeatureFlag> GetAllFeatures();
    }
   
}