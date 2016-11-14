using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using FeatureFlags.Evaluator;
using FeatureFlags.FeatureFlag;

namespace FeatureFlags.Stores.AppSettings
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

        public AppSettingsFeatureStore(): this("features.", ConfigurationManager.AppSettings, false)
        {
        }

        public AppSettingsFeatureStore(NameValueCollection collection) : this("features.", collection, false)
        {
        }

        public AppSettingsFeatureStore(string prefix) : this(prefix, ConfigurationManager.AppSettings, false)
        {
        }

        public IFeatureFlag GetFeature(string featureKey)
        {
            if (Dynamic)
                return new DynamicFeatureFlag(featureKey, this);

            return new StaticFeatureFlag(featureKey, GetFeatureState(featureKey, null));

        }

        public IEnumerable<IFeatureFlag> GetAllFeatures()
        {
            return Collection.AllKeys.Select(GetFeature);
        }

        public FeatureFlagState GetFeatureState(string featureKey, FeatureContext featureContext)
        {
            var v = GetFeatureFlagDefinition(featureKey)?.Definition;
            return FeatureFlagEvaluatorUtils.Parse(featureKey, v).Evaluate(featureContext);
        }

        public FeatureFlagDefinition GetFeatureFlagDefinition(string featureKey)
        {
            return new FeatureFlagDefinition
            {
                Name = featureKey,
                Definition = Collection.Get(Prefix + featureKey)
            };
        }

        public void SetFeatureFlagDefinition(FeatureFlagDefinition featureFlagDefinition)
        {
            throw new System.NotImplementedException();
        }
    }
}