using System;
using System.Diagnostics;
using System.Threading;

namespace FeatureFlags
{
    public static class Features
    {
        public static IFeatureStore FeatureStore { get; set; }

        private static readonly AsyncLocal<FeatureContext> AsyncLocalFeatureContext = new AsyncLocal<FeatureContext>();
        private static readonly ThreadLocal<FeatureContext> ThreadLocalFeatureContext = new ThreadLocal<FeatureContext>();

        public static FeatureContext AmbientContext
        {
            get
            {
                var a = AsyncLocalFeatureContext.Value;
                if (a != null)
                {
                    Debug.WriteLine("Context found in AsyncLocal");
                    return a;
                }
                a = ThreadLocalFeatureContext.Value;
                if (a != null)
                {
                    Debug.WriteLine("Context found in ThreadLocal");
                    return a;
                }
                return null;
            }
            set
            {
                AsyncLocalFeatureContext.Value = value;
                //ThreadLocalFeatureContext.Value = value;
            }
        }

        public static bool IsActive(string featureKey, FeatureContext featureContext)
        {
            return FeatureStore.GetFeature(featureKey).IsActive(featureContext);
        }

        public static bool IsActive(string featureKey)
        {
            return IsActive(featureKey, AmbientContext);
        }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public class FeatureFlagAttribute : Attribute
    {
        public string FeatureKey { get; set; }

        public FeatureFlagAttribute(string featureKey)
        {
            FeatureKey = featureKey;
        }
        public FeatureFlagAttribute()
        {
            FeatureKey = null;
        }
    }

    public static class SomeFeatures
    {
        [FeatureFlag]
        public static bool FeatureA { get; }

        [FeatureFlag]
        public static bool FeatureB { get; }
        
        [FeatureFlag("FeatureC")]
        public static bool TheFeatureC { get; }
    }

}