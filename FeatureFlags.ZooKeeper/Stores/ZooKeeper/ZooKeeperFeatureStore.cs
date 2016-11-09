using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeatureFlags.Evaluator;
using FeatureFlags.FeatureFlag;
using org.apache.zookeeper;
using Z = org.apache.zookeeper;

namespace FeatureFlags.Stores.ZooKeeper
{
    public class ZooKeeperFeatureStore : IFeatureStore, IDynamicFeatureStore, IWatchdog
    {
        readonly object connectionSetupLock=new object();
        readonly string prefix = "/features";
        readonly string connectionString;
        readonly bool useWatchdog;

        Z.ZooKeeper zooKeeper;
        FeatureWatcher watcher;

        public ZooKeeperFeatureStore(string zkConnectionString): this(zkConnectionString, true)
        {
        }

        public ZooKeeperFeatureStore(string zkConnectionString, bool useWatchdogParam)
        {
            connectionString = zkConnectionString;
            useWatchdog = useWatchdogParam;
            SetupConnection();
        }

        public IFeatureFlag GetFeature(string featureKey)
        {
            var state = zooKeeper.getState();
            if ((state == Z.ZooKeeper.States.CONNECTING || state == Z.ZooKeeper.States.CONNECTED || state == Z.ZooKeeper.States.CONNECTEDREADONLY) && !localView.ContainsKey(featureKey))
                AsyncHelper.RunSync(()=>SetupKey(featureKey));
            return new DynamicFeatureFlag(featureKey, this);
        }

        public IEnumerable<IFeatureFlag> GetAllFeatures()
        {
            var state = zooKeeper.getState();
            if ((state == Z.ZooKeeper.States.CONNECTING || state == Z.ZooKeeper.States.CONNECTED || state == Z.ZooKeeper.States.CONNECTEDREADONLY))
                AsyncHelper.RunSync(() => SetupAllKeys());
            return localView.Keys.Select(x => new DynamicFeatureFlag(x, this));
        }

        #region IDynamicFeatureStore implementation
        public FeatureFlagState GetFeatureState(string name, FeatureContext featureContext)
        {
            LocalViewItem ret;
            if (localView.TryGetValue(name, out ret))
                return ret.Evaluator.Evaluate(featureContext);
            return FeatureFlagState.Inactive;
        }

        public FeatureFlagDefinition GetFeatureFlagDefinition(string featureKey)
        {
            LocalViewItem ret;
            if (localView.TryGetValue(featureKey, out ret))
                return new FeatureFlagDefinition
                {
                    Name = featureKey,
                    Definition = ret.Definition
                };
            return null;
        }

        public void SetFeatureFlagDefinition(FeatureFlagDefinition featureFlagDefinition)
        {
            if (zooKeeper.getState() == Z.ZooKeeper.States.CONNECTED || zooKeeper.getState()==Z.ZooKeeper.States.CONNECTING)
                WriteKey(featureFlagDefinition.Name, featureFlagDefinition.Definition).Wait();
            else
                localView[featureFlagDefinition.Name] = new LocalViewItem
                {
                    Definition = featureFlagDefinition.Definition,
                    Evaluator = FeatureFlagEvaluatorUtils.Parse(featureFlagDefinition.Definition)
                };
        }

        #endregion

        #region Local Cache
        private readonly ConcurrentDictionary<string, LocalViewItem> localView = new ConcurrentDictionary<string, LocalViewItem>();

        #endregion

        #region Zookeeper connection management
        private void SetupConnection()
        {
            lock (connectionSetupLock)
            {
                AsyncHelper.RunSync(SetupConnectionAsync);
            }
        }

        private async Task SetupConnectionAsync()
        {
            zooKeeper = new org.apache.zookeeper.ZooKeeper(connectionString, 100000, new ZookeeperWatcher());
            org.apache.zookeeper.ZooKeeper.LogToFile = false;
            org.apache.zookeeper.ZooKeeper.LogToTrace = true;

            var root = await zooKeeper.existsAsync("/");
            if (root == null)
                await zooKeeper.createAsync("/", null, Z.ZooDefs.Ids.OPEN_ACL_UNSAFE, Z.CreateMode.PERSISTENT);

            await CreateIfNotExist(zooKeeper, prefix);
            await CreateIfNotExist(zooKeeper, watchdogPrefix);

            watcher = new FeatureWatcher(zooKeeper, OnZookeeperEntryChanged);
            if (useWatchdog)
                await SetupWatchdog();
            localView.Clear();
        }

        private async Task<T> TryAndRetry<T>(Func<Z.ZooKeeper,Task<T>> func)
        {
            return await func(zooKeeper);
            /*
            var z = zooKeeper;
            int counter = 10;
            while (true)
            {
                try
                {
                    return await func(z);
                }
                catch (Z.KeeperException.ConnectionLossException)
                {
                    if (counter <= 0)
                        throw;
                }
                catch (Z.KeeperException.SessionExpiredException)
                {
                    if (counter <= 0)
                        throw;
                }
                Debug.WriteLine("ZookeeperRestart");
                SetupConnection();
                z = zooKeeper;
                counter--;
            }*/
        }

        private async Task TryAndRetry(Func<Z.ZooKeeper, Task> func)
        {
            await func(zooKeeper);
            return;
/*            var z = zooKeeper;
            int delay = 500;
            int iter = 1;
            int iterprev = 0;
            while (true)
            {
                try
                {
                    await func(z);
                    return;
                }
                catch (Z.KeeperException.ConnectionLossException)
                {

                }
                catch (Z.KeeperException.SessionExpiredException)
                {

                }
                Debug.WriteLine("ZookeeperRestart");
                await Task.Delay(iter*delay);
                var iterold = iter;
                iter = iter+iterprev;
                iterprev = iterold;
                SetupConnection();
                z = zooKeeper;
            }*/
        }


        private async Task WriteKey(string name, string definition)
        {
            var path = GetPathForKey(name);
            await TryAndRetry(async z =>
            {
                var stat = await z.existsAsync(path);
                if (stat == null)
                    await CreateIfNotExist(z, path, definition);
                else
                    await z.setDataAsync(path, Encoding.UTF8.GetBytes(definition));
            });
        }

        private async Task SetupAllKeys()
        {
            var list = await TryAndRetry(async z => await z.getChildrenAsync(prefix));
            await Task.WhenAll(list.Children.Select(SetupKey).ToArray());
        }
        #endregion


        private void OnZookeeperEntryChanged(string path, string data)
        {
            var featureKey = GetKeyForPath(path);
            if (featureKey != null)
            {
                localView[featureKey] = new LocalViewItem { Definition = data, Evaluator = FeatureFlagEvaluatorUtils.Parse(data) };
            }
        }

        private async Task SetupKey(string featureKey)
        {
            var path = GetPathForKey(featureKey);

/*            await TryAndRetry(async z =>
            {
                var doLoop = false;
                do
                {
                */
                    try
                    {
                        var res = await zooKeeper.getDataAsync(path, watcher);
                        OnZookeeperEntryChanged(path, res.Data == null ? null : Encoding.UTF8.GetString(res.Data));
                    }
                    catch (KeeperException.NoNodeException)
                    {
                        // setup watcher to catch node creation
                        if (await zooKeeper.existsAsync(path, watcher) == null)
                            OnZookeeperEntryChanged(path, null);
//                        else
//                            doLoop = true; // if entry created in between
                    }
/*
                } while (doLoop);

            });*/
        }

        private string GetPathForKey(string featureKey)
        {
            return prefix + "/" + featureKey.Replace('/', '\\');
        }

        private string GetKeyForPath(string path)
        {
            if (path != null && path.StartsWith(prefix + "/", StringComparison.Ordinal))
                return path.Substring(10).Replace('\\', '/');
            return null;
        }


        #region Zookeeper helpers

        private static async Task<string> CreateIfNotExist(Z.ZooKeeper zk, string path, string data = null)
        {
            var p = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var s = "";
            string ret = null;

            for (int i = 0; i < p.Length; i++)
            {
                s = s + "/" + p[i];
                try
                {
                    ret = await zk.createAsync(s, i == p.Length - 1 && data!=null ? Encoding.UTF8.GetBytes(data) : null, Z.ZooDefs.Ids.OPEN_ACL_UNSAFE,
                        Z.CreateMode.PERSISTENT);
                }
                catch (Z.KeeperException.NodeExistsException)
                {
                }
            }
            return ret;
        }

        #endregion

        #region watchdog

        private readonly string watchdogPrefix="/watchdog";

        private Task SetupWatchdog()
        {
            var s = watchdogPrefix + "/" + InstanceId;

            TryAndRetry(async z =>
            {
                await CreateIfNotExist(z, s);
                await z.createAsync(s + "/n", null, Z.ZooDefs.Ids.OPEN_ACL_UNSAFE, Z.CreateMode.EPHEMERAL_SEQUENTIAL);
            });

            return Task.CompletedTask;
        }

        public string InstanceId
        {
            get { return System.Environment.MachineName; }
        }

        public async Task<List<string>> GetActiveNodes()
        {
            return await TryAndRetry(async z =>
            {
                var l = await z.getChildrenAsync(watchdogPrefix);
                var ret = new List<string>();
                foreach (var item in l.Children)
                {
                    var actives = await z.getChildrenAsync(watchdogPrefix + "/" + item);
                    ret.AddRange(actives.Children.Select(active => item + "/" + active));
                }
                return ret;
            });
        }

#endregion
    }

    public static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new
          TaskFactory(CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return AsyncHelper._myTaskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            AsyncHelper._myTaskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }

}