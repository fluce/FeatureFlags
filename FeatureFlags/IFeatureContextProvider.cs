namespace FeatureFlags
{
    public interface IFeatureContextProvider
    {
        FeatureContext GetContext();
    }
}