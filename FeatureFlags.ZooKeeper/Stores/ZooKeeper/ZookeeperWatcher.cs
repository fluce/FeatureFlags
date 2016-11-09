using System.Diagnostics;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace FeatureFlags.Stores.ZooKeeper
{
    class ZookeeperWatcher : Watcher
    {
        public override Task process(WatchedEvent @event)
        {
            Debug.WriteLine($"{@event.getPath()} : {@event.get_Type()} : {@event.getState()}");
            return Task.CompletedTask;
        }
    }
}