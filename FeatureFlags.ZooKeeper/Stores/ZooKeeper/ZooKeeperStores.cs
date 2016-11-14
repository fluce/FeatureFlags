using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeatureFlags.ZooKeeper.Stores.ZooKeeper
{
    public class ZooKeeperStores
    {
        readonly string connectionString;

        public ZooKeeperStores(string zkConnectionString)
        {
            connectionString = zkConnectionString;
        }

        public async Task<List<string>> GetStores()
        {
            var zooKeeper = new org.apache.zookeeper.ZooKeeper(connectionString, 100000, null, true);
            org.apache.zookeeper.ZooKeeper.LogToFile = false;
            org.apache.zookeeper.ZooKeeper.LogToTrace = true;

            var list = await zooKeeper.getChildrenAsync("/");
            
            var all=list.Children.Select(x=> new { Key=x, Exist=zooKeeper.existsAsync("/"+x+"/features") }).ToArray();
            await Task.WhenAll(all.Select(x => x.Exist));

            return all.Where(x => x.Exist.Status == TaskStatus.RanToCompletion && x.Exist.Result != null)
                .Select(x => x.Key)
                .ToList();

        }
    }
}
