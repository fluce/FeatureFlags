using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FeatureFlags.Stores;

namespace FeatureFlags
{
    public class Features: IFeatures
    {
        public IFeatureContextProvider FeatureContextProvider { get; set; }

        public IFeatureStore FeatureStore { get; set; }

        private static object _lock=new object();

        public static IFeatures Current { get; private set; }


        public static void Setup(IFeatureStore store, IFeatureContextProvider contextProvider)
        {
            Current = new Features(store,contextProvider);
        }

        public Features(IFeatureStore store, IFeatureContextProvider contextProvider)
        {
            FeatureStore = store;
            FeatureContextProvider = contextProvider;
        }

        public IFeatureContextProvider ContextProvider => FeatureContextProvider;

        public bool IsActive(string featureKey, FeatureContext featureContext)
        {
            if (featureContext.InternalFeatureContext==null)
                featureContext.InternalFeatureContext = new InternalFeatureContext() { Features = this };

            return FeatureStore.GetFeature(featureKey).IsActive(featureContext);
        }

        public bool IsActive(string featureKey)
        {
            return IsActive(featureKey, FeatureContextProvider?.GetContext());
        }

        public IEnumerable<IFeatureFlag> GetAllFeatures()
        {
            var a=FeatureStore.GetAllFeatures().ToArray();
            return a; // a.Select(x => x.Name);
        }
    }
}