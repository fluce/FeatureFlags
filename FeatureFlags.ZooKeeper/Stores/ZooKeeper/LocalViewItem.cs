using FeatureFlags.Evaluator;

namespace FeatureFlags.Stores.ZooKeeper
{
    class LocalViewItem
    {
        public string Definition { get; set; }
        public FeatureFlagStateEvaluator Evaluator { get; set; }
    }
}