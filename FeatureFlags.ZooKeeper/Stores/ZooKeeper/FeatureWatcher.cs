using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace FeatureFlags.Stores.ZooKeeper
{
    class FeatureWatcher : Watcher
    {
        readonly org.apache.zookeeper.ZooKeeper zooKeeper;
        readonly Action<string, string> onChange;

        public FeatureWatcher(org.apache.zookeeper.ZooKeeper zk, Action<string, string> onChangeAction)
        {
            zooKeeper = zk;
            onChange = onChangeAction;
        }

        public override async Task process(WatchedEvent @event)
        {
            var path = @event.getPath();

            Debug.WriteLine($"ZK event {@event.get_Type()} on {path}");

            if (@event.get_Type() == Event.EventType.NodeDeleted)
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