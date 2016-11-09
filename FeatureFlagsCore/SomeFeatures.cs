namespace FeatureFlags
{
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