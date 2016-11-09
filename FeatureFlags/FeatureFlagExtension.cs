using FeatureFlags.FeatureFlag;

namespace FeatureFlags
{
    public static class FeatureFlagExtension
    {
        public static bool IsActive(this IFeatureFlag feature, FeatureContext context)
        {
            return feature.GetState(context) == FeatureFlagState.Active;
        }

        public static bool IsActive(this IFeatureFlag feature)
        {
            return feature.GetState(null) == FeatureFlagState.Active;
        }

    }
}