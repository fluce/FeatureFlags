namespace FeatureFlags
{
    public interface IFeatureStore
    {
        IFeatureFlag GetFeature(string featureKey);
    }
}