using System.Collections.Specialized;

namespace FeatureFlags
{
    public class AppSettingsFeatureStore : IFeatureStore, IDynamicFeatureStore
    {
        public string Prefix { get; private set; }
        public NameValueCollection Collection { get; private set; }
        public bool Dynamic { get; private set; }

        public AppSettingsFeatureStore(string prefix, NameValueCollection collection, bool dynamic)
        {
            Prefix = prefix;
            Collection = collection;
            Dynamic = dynamic;
        }

        public AppSettingsFeatureStore(NameValueCollection collection) : this("features.", collection, false)
        {
        }

/*
        public AppSettingsFeatureStore(): this("features.", ConfigurationManager.AppSettings, false)
        {
        }

        public AppSettingsFeatureStore(string prefix) : this(prefix, ConfigurationManager.AppSettings, false)
        {
        }
*/
        public IFeatureFlag GetFeature(string featureKey)
        {
            if (Dynamic)
                return new DynamicFeatureFlag(featureKey, this);

            return new StaticFeatureFlag(featureKey, GetFeatureState(featureKey, null));

        }

        public FeatureFlagState GetFeatureState(string featureKey, FeatureContext featureContext)
        {
            var v = Collection.Get(Prefix + featureKey);
            return FeatureFlagEvaluatorUtils.Parse(v).Evaluate(featureContext);
        }
    }
}