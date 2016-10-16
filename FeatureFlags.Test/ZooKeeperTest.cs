using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using org.apache.zookeeper;

namespace FeatureFlags.Test
{
    [TestFixture]
    public class ZooKeeperTest
    {
        IFeatureStore featureStore;

        [OneTimeSetUp]
        public async Task Setup()
        {
            StartZooKeeper();
            var zk = new ZooKeeper("127.0.0.1:2181", 60000, null);
            await DeleteRecursive(zk, "/FeatureFlags", "features");
            await CreateIfNotExist(zk, "/FeatureFlags/features/featureA", "true");
            await CreateIfNotExist(zk, "/FeatureFlags/features/featureB", "false");
            await CreateIfNotExist(zk, "/FeatureFlags/features/featureC", "true");
            await CreateIfNotExist(zk, "/FeatureFlags/features/featureD", "true");
            await zk.closeAsync();

            featureStore = new CachingFeatureStore(new ZooKeeperFeatureStore("127.0.0.1:2181/FeatureFlags"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            StopZookeeper();
        }

        public void StartZooKeeper()
        {
            using (PowerShell powerShellInstance = PowerShell.Create())
            {
                powerShellInstance.AddScript($"cd '{TestContext.CurrentContext.TestDirectory}\\docker'");
                powerShellInstance.AddScript("docker-compose up -d");
                var ret = powerShellInstance.Invoke();
            }
        }
        public void StopZookeeper()
        {
            using (PowerShell powerShellInstance = PowerShell.Create())
            {
                powerShellInstance.AddScript($"cd '{TestContext.CurrentContext.TestDirectory}\\docker'");
                powerShellInstance.AddScript("docker-compose down");
                var ret = powerShellInstance.Invoke();
            }
        }

        [Test]
        public void ZooKeeperFeatureIsActive()
        {
            Assert.IsTrue(featureStore.GetFeature("featureA").IsActive());
        }

        [Test]
        public void ZooKeeperFeatureIsActiveSecondTime()
        {
            Assert.IsTrue(featureStore.GetFeature("featureA").IsActive());
        }

        [Test]
        public void ZooKeeperFeatureIsInactive()
        {
            Assert.IsFalse(featureStore.GetFeature("featureB").IsActive());
        }

        [Test]
        public void ZooKeeperFeatureIsMissing()
        {
            Assert.IsFalse(featureStore.GetFeature("featureMissing").IsActive());
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public async Task ZooKeeperFeatureChanging()
        {
            Assert.IsTrue(featureStore.GetFeature("featureA").IsActive());
            await ChangeValue("/FeatureFlags/features/featureA", "false");
            await Task.Delay(50);
            Assert.IsFalse(featureStore.GetFeature("featureA").IsActive());
            await ChangeValue("/FeatureFlags/features/featureA", "true");
            await Task.Delay(50);
            Assert.IsTrue(featureStore.GetFeature("featureA").IsActive());
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public async Task ZooKeeperFeatureRemoved()
        {
            Assert.IsTrue(featureStore.GetFeature("featureC").IsActive());
            await DeleteValue("/FeatureFlags/features/featureC");
            //await Task.Delay(100);
            Assert.IsFalse(featureStore.GetFeature("featureC").IsActive());
        }


        [Test]
        [Parallelizable(ParallelScope.None)]
        public async Task ZooKeeperFeatureAdded()
        {
            try
            {
                await DeleteValue("/FeatureFlags/features/featureAdded");
            } catch (KeeperException.NoNodeException) { }

            Assert.IsFalse(featureStore.GetFeature("featureAdded").IsActive());

            await AddValue("/FeatureFlags/features/featureAdded", "true");
            await Task.Delay(100);

            Assert.IsTrue(featureStore.GetFeature("featureAdded").IsActive());

            await DeleteValue("/FeatureFlags/features/featureAdded");
            await Task.Delay(100);

            Assert.IsFalse(featureStore.GetFeature("featureAdded").IsActive());
        }

        [Test, Explicit]
        [Parallelizable(ParallelScope.None)]
        public async Task ZooKeeperCheckIfWatcherReconnectAfterDisconnecting()
        {
            Assert.IsTrue(featureStore.GetFeature("featureD").IsActive());

            await ChangeValue("/FeatureFlags/features/featureD", "false");
            await Task.Delay(50);
            Assert.IsFalse(featureStore.GetFeature("featureD").IsActive());

            await Task.Delay(1000);
            Debug.WriteLine("Stopping Zookeeper");
            StopZookeeper();
            Debug.WriteLine("Zookeeper stopped");
            await Task.Delay(10000);
            Debug.WriteLine("Starting Zookeeper");
            StartZooKeeper();
            Debug.WriteLine("Zookeeper started");
            await Task.Delay(1000);

            Assert.IsFalse(featureStore.GetFeature("featureD").IsActive());

            await ChangeValue("/FeatureFlags/features/featureD", "true");
            await Task.Delay(150);

            Assert.IsTrue(featureStore.GetFeature("featureD").IsActive());
        }

        [Test, Explicit]
        [Parallelizable(ParallelScope.None)]
        public async Task ZooKeeperCheckIfFeatureStillActiveWhileDisconnected()
        {
            Assert.IsTrue(featureStore.GetFeature("featureD").IsActive());

            for (int i = 0; i < 10; i++) // manually kill zookeeper and restart it
            {
                if (i == 3) StopZookeeper();
                if (i == 7) StartZooKeeper();

                await Task.Delay(500);
                Assert.IsTrue(featureStore.GetFeature("featureD").IsActive());
            }

        }



        public async Task ChangeValue(string path, string value)
        {
            var zk = new ZooKeeper("127.0.0.1:2181", 60000, null);
            await zk.setDataAsync(path, Encoding.UTF8.GetBytes(value));
            await zk.closeAsync();
        }

        public async Task AddValue(string path, string value)
        {
            var zk = new ZooKeeper("127.0.0.1:2181", 60000, null);
            await zk.createAsync(path, Encoding.UTF8.GetBytes(value), ZooDefs.Ids.OPEN_ACL_UNSAFE,
                        CreateMode.PERSISTENT);
            await zk.closeAsync();
        }

        public async Task DeleteValue(string path)
        {
            var zk = new ZooKeeper("127.0.0.1:2181", 60000, null);
            await zk.deleteAsync(path);
            await zk.closeAsync();
        }


        static async Task DeleteRecursive(ZooKeeper zk, string path = "", string key = "")
        {
            try
            {
                var correctedPath = path + "/" + key;
                var a = await zk.getChildrenAsync(correctedPath);

                foreach (var child in a.Children)
                {
                    await DeleteRecursive(zk, correctedPath == "/" ? "" : correctedPath, child);
                }
                await zk.deleteAsync(correctedPath);
            } catch (KeeperException.NoNodeException) { }
        }

        static async Task<string> CreateIfNotExist(ZooKeeper zk, string path, string data=null)
        {
            var p = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var s = "";
            string ret = null;

            for (int i = 0; i < p.Length; i++)
            {
                s = s + "/" + p[i];
                try
                {
                    ret = await zk.createAsync(s, i == p.Length - 1 ? Encoding.UTF8.GetBytes(data) : null, ZooDefs.Ids.OPEN_ACL_UNSAFE,
                        CreateMode.PERSISTENT);
                }
                catch (KeeperException.NodeExistsException)
                {
                }
            }
            return ret;
        }

    }
}
