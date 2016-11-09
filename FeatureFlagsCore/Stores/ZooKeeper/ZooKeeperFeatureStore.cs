using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace FeatureFlags
{
    public class ZooKeeperFeatureStore : IFeatureStore, IDynamicFeatureStore
    {
        readonly ZooKeeper zooKeeper;
        readonly FeatureWatcher watcher;
        readonly string prefix = "/features/";

        public ZooKeeperFeatureStore(string zkConnectionString)
        {
            zooKeeper = new ZooKeeper(zkConnectionString, 10000, new ZookeeperWatcher());
            watcher = new FeatureWatcher(zooKeeper, OnZookeeperEntryChanged);
        }

        public IFeatureFlag GetFeature(string featureKey)
        {
            setupKey(featureKey).Wait();
            return new DynamicFeatureFlag(featureKey, this);
        }

        private readonly ConcurrentDictionary<string, FeatureFlagStateEvaluator> localView = new ConcurrentDictionary<string, FeatureFlagStateEvaluator>();

        public FeatureFlagState GetFeatureState(string name, FeatureContext featureContext)
        {
            FeatureFlagStateEvaluator ret;
            if (localView.TryGetValue(name, out ret))
                return ret.Evaluate(featureContext);
            return FeatureFlagState.Inactive;
        }

        private void OnZookeeperEntryChanged(string path, string data)
        {
            var featureKey = GetKeyForPath(path);
            if (featureKey != null)
            {
                localView[featureKey] = FeatureFlagEvaluatorUtils.Parse(data);
            }
        }

        private async Task setupKey(string featureKey)
        {
            var path = GetPathForKey(featureKey);
            var doLoop = false;

            do
            {

                try
                {
                    await zooKeeper.getDataAsync(path, watcher).ContinueWith(
                        d =>
                        {
                            OnZookeeperEntryChanged(path, Encoding.UTF8.GetString(d.Result.Data));
                        }
                        );
                }
                catch (AggregateException e) when (e.InnerException is KeeperException.NoNodeException)
                {
                    // setup watcher to catch node creation
                    if (await zooKeeper.existsAsync(path, watcher) == null)
                        OnZookeeperEntryChanged(path, null);
                    else
                        doLoop = true; // if entry created in between
                }

            } while (doLoop);
        }

        private string GetPathForKey(string featureKey)
        {
            return prefix + featureKey.Replace('/', '\\');
        }

        private string GetKeyForPath(string path)
        {
            if (path != null && path.StartsWith(prefix, StringComparison.Ordinal))
                return path.Substring(10).Replace('\\', '/');
            return null;
        }

        private class ZookeeperWatcher : Watcher
        {
            public override Task process(WatchedEvent @event)
            {
                Debug.WriteLine($"{@event.getPath()} : {@event.get_Type()} : {@event.getState()}");
                return Task.CompletedTask;
            }
        }

        private class FeatureWatcher : Watcher
        {
            readonly ZooKeeper zooKeeper;
            readonly Action<string, string> onChange;

            public FeatureWatcher(ZooKeeper zk, Action<string, string> onChangeAction)
            {
                zooKeeper = zk;
                onChange = onChangeAction;
            }

            public override async Task process(WatchedEvent @event)
            {
                var path = @event.getPath();

                if (@event.get_Type()==Event.EventType.NodeDeleted)
                    onChange(path, null);

                if (@event.get_Type() == Event.EventType.NodeDataChanged || @event.get_Type() == Event.EventType.NodeCreated)
                {
                    try
                    {
                        var data = await zooKeeper.getDataAsync(path, this);
                        onChange(path, Encoding.UTF8.GetString(data.Data));
                    }
                    catch (KeeperException.NoNodeException)
                    {
                        onChange(path, null);
                    }
                }
            }
        }


    }
}