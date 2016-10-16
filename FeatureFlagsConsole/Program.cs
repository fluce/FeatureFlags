using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace FeatureFlagsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var zk = new ZooKeeper("127.0.0.1:2181", 60000, null);

            TestCreate(zk).Wait();

            TestWatch(zk).Wait();

            DumpData(zk).Wait();

            Console.ReadKey();
        }

        static async Task DumpData(ZooKeeper zk, string path="", string key="", int level=1)
        {
            var correctedPath = path + "/" + key;
            var a = await zk.getChildrenAsync(correctedPath);
            var d = await zk.getDataAsync(correctedPath);

            Console.WriteLine(new string(' ', (level) * 2) + key);
            if (d!=null && d.Data!=null && d.Data.Length > 0)
                Console.WriteLine(new string(' ', (level+1)*2) + "D: " + Encoding.UTF8.GetString(d.Data));

            foreach (var child in a.Children)
            {
                await DumpData(zk, correctedPath=="/"?"":correctedPath, child, level + 1);
            }

        }

        static async Task DeleteRecursive(ZooKeeper zk, string path = "", string key = "")
        {
            var correctedPath = path + "/" + key;
            var a = await zk.getChildrenAsync(correctedPath);
            var d = await zk.getDataAsync(correctedPath);

            foreach (var child in a.Children)
            {
                await DeleteRecursive(zk, correctedPath == "/" ? "" : correctedPath, child);
            }
                await zk.deleteAsync(correctedPath);

        }

        static async Task<string> CreateIfNotExist(ZooKeeper zk, string path, byte[] data)
        {
            var p=path.Split(new[] {'/'},StringSplitOptions.RemoveEmptyEntries);
            var s = "";
            string ret=null;

            for (int i=0;i<p.Length;i++)
            {
                s = s + "/" + p[i];
                try
                {
                    ret=await zk.createAsync(s, i==p.Length-1?data:null, ZooDefs.Ids.OPEN_ACL_UNSAFE,
                        CreateMode.PERSISTENT);
                }
                catch (KeeperException.NodeExistsException)
                {
                }
            }
            return ret;
        }

        static async Task TestCreate(ZooKeeper zk)
        {
            try
            {
                try
                {
                    await DeleteRecursive(zk,"","featureflags");
                }
                catch (KeeperException)
                {
                }
                var s = await zk.createAsync("/featureflags", null, ZooDefs.Ids.OPEN_ACL_UNSAFE,
                    CreateMode.PERSISTENT);
                Console.WriteLine($"Record created : {s}");
                s = await CreateIfNotExist(zk, "/featureflags/features/theFeatureA", new[] { (byte)0x31 });
                Console.WriteLine($"Record created : {s}");
                s = await CreateIfNotExist(zk, "/featureflags/features/theFeatureB", new[] { (byte)0x32 });
                Console.WriteLine($"Record created : {s}");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error : "+e.Message);
            }
        }

        public class MyWatcher : Watcher
        {
            readonly ZooKeeper zk;
            public MyWatcher(ZooKeeper _zk)
            {
                zk = _zk;
            }

            public override async Task process(WatchedEvent @event)
            {
                Console.WriteLine($"EVENT : {@event.get_Type()} {@event.getPath()} {@event.getState()}");
                await zk.getDataAsync(@event.getPath(), this);

            }
        }

        static async Task TestWatch(ZooKeeper zk)
        {
            try
            {
                await zk.getDataAsync("/featureflags/features/theFeatureB", new MyWatcher(zk));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error : " + e.Message);
            }

            for (byte i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                Console.WriteLine("Modif !");
                await zk.setDataAsync("/featureflags/features/theFeatureB", new byte[] {(byte) (0x31 + i)});
            }


        }

    }
}
