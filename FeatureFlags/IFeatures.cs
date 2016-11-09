namespace FeatureFlags
{
    public interface IFeatures
    {
        IFeatureContextProvider ContextProvider { get; }
        bool IsActive(string featureKey, FeatureContext featureContext);
        bool IsActive(string featureKey);
    }
}