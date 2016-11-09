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
}