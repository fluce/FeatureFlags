using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatureFlags.AspNetCore;
using FeatureFlags.Stores.ZooKeeper;

namespace FeatureFlags.Stores.ZooKeeper.AspNetCore
{
    public static class FeatureFlagsConfigExtension
    {
        public static FeatureFlagsConfig UseZooKeeperFeatureStore(this FeatureFlagsConfig config,
            string connectionString)
        {
            config.Store = new ZooKeeperFeatureStore(connectionString);
            return config;
        }
    }
}
