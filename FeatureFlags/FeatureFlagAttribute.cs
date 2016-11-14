using System;

namespace FeatureFlags
{
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

    [AttributeUsage(AttributeTargets.Interface)]
    public class FeatureFlagPrefixAttribute : Attribute
    {
        public string FeatureKeyPrefix { get; set; }

        public FeatureFlagPrefixAttribute(string featureKeyPrefix)
        {
            FeatureKeyPrefix = featureKeyPrefix;
        }
        public FeatureFlagPrefixAttribute()
        {
            FeatureKeyPrefix = null;
        }
    }

}