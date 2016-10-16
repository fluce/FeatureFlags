using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using org.apache.zookeeper;

namespace FeatureFlags.Test
{
    [TestFixture]
    public class WeaverTest
    {
        [Test]
        public void GetFeatureFlag()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureA", "true");
            n.Add("features.FeatureC", "true");

            Features.FeatureStore= new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true));

            Assert.IsTrue(SomeFeatures.FeatureA);
            Assert.IsFalse(SomeFeatures.FeatureB);
            Assert.IsTrue(SomeFeatures.TheFeatureC);
        }
    }

    [TestFixture]
    public class AmbientContext
    {
        [SetUp]
        public void Setup()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureA", "true");
            n.Add("features.FeatureC", @"
{
    'contextualRules': [
        {
            'rule': 'Schedule',
            'from': '2016-11-28T12:00:00',
            'to': '2016-11-28T14:00:00'
        }
    ]
}"
            );

            Features.FeatureStore = new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true));
        }

        [Test]
        public void ThreadStaticContextSameThread()
        {
            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            Assert.IsTrue(SomeFeatures.TheFeatureC);

            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 15, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            Assert.IsFalse(SomeFeatures.TheFeatureC);

        }

        [Test]
        public void ThreadStaticContextOtherThread()
        {
            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            bool wasActive = false;
            int wasThreadId = Thread.CurrentThread.ManagedThreadId;
            var t =new Thread(() =>
            {
                wasActive = SomeFeatures.TheFeatureC;
                wasThreadId = Thread.CurrentThread.ManagedThreadId;
            });
            t.Start();

            t.Join();

            Assert.IsTrue(wasActive);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadId);

        }

        [Test]
        public void ThreadStaticContextOtherThreadFromPool()
        {
            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            wasActiveThreadStaticContextOtherThreadFromPool = false;
            wasThreadIdThreadStaticContextOtherThreadFromPool = Thread.CurrentThread.ManagedThreadId;
            ThreadPool.QueueUserWorkItem(CallBack);
            Thread.Sleep(500);
            Assert.IsTrue(wasActiveThreadStaticContextOtherThreadFromPool);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadIdThreadStaticContextOtherThreadFromPool);

        }

        bool wasActiveThreadStaticContextOtherThreadFromPool;
        int wasThreadIdThreadStaticContextOtherThreadFromPool;
        private void CallBack(object state)
        {
            wasActiveThreadStaticContextOtherThreadFromPool = SomeFeatures.TheFeatureC;
            wasThreadIdThreadStaticContextOtherThreadFromPool = Thread.CurrentThread.ManagedThreadId;
        }


        [Test]
        public void ThreadStaticContextOtherThreadTask()
        {
            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            var tf = new TaskFactory();

            bool wasActive = false;
            int wasThreadId = Thread.CurrentThread.ManagedThreadId;
            var t = tf.StartNew(() => {
                wasActive = SomeFeatures.TheFeatureC;
                wasThreadId = Thread.CurrentThread.ManagedThreadId;
            });
            t.Wait();
            Assert.IsTrue(wasActive);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadId);

        }

        [Test]
        public async Task ThreadStaticContextOtherThreadAsync()
        {
            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            int originalThreadId = Thread.CurrentThread.ManagedThreadId;

            await Task.Delay(1000);

            int secondThreadId = Thread.CurrentThread.ManagedThreadId;

            Assert.IsTrue(SomeFeatures.TheFeatureC);
            Assert.AreNotEqual(originalThreadId, secondThreadId);

        }


        [Test]
        public void ThreadStaticContextTwoThreadTwoContext()
        {
            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            var tf = new TaskFactory();

            bool wasActive1 = false;
            int wasThreadId1 = Thread.CurrentThread.ManagedThreadId;

            var t1 = tf.StartNew(() => {
                Thread.Sleep(500);
                wasActive1 = SomeFeatures.TheFeatureC;
                wasThreadId1 = Thread.CurrentThread.ManagedThreadId;
                Thread.Sleep(500);
            });

            Features.AmbientContext = new FeatureContext
            {
                DateTime = new DateTime(2016, 11, 28, 15, 00, 00),
                Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
            };

            bool wasActive2 = true;
            int wasThreadId2 = Thread.CurrentThread.ManagedThreadId;

            var t2 = tf.StartNew(() => {
                Thread.Sleep(500);
                wasActive2 = SomeFeatures.TheFeatureC;
                wasThreadId2 = Thread.CurrentThread.ManagedThreadId;
                Thread.Sleep(500);
            });



            t1.Wait();
            t2.Wait();
            Assert.IsTrue(wasActive1);
            Assert.IsFalse(wasActive2);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadId1);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadId2);

        }

        [Test]
        public void ThreadStaticContextTwoThreadTwoInThreadContext()
        {
            var tf = new TaskFactory();

            bool wasActive1 = false;
            int wasThreadId1 = Thread.CurrentThread.ManagedThreadId;

            var t1 = tf.StartNew(() => {
                Features.AmbientContext = new FeatureContext
                {
                    DateTime = new DateTime(2016, 11, 28, 13, 00, 00),
                    Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
                };
                Thread.Sleep(500);
                wasActive1 = SomeFeatures.TheFeatureC;
                wasThreadId1 = Thread.CurrentThread.ManagedThreadId;
                Thread.Sleep(500);
            });

            bool wasActive2 = true;
            int wasThreadId2 = Thread.CurrentThread.ManagedThreadId;

            var t2 = tf.StartNew(() => {
                Features.AmbientContext = new FeatureContext
                {
                    DateTime = new DateTime(2016, 11, 28, 15, 00, 00),
                    Uid = Guid.Parse("B783BB98-CEE3-4A6F-A57F-B1A54B57ECE5")
                };
                Thread.Sleep(500);
                wasActive2 = SomeFeatures.TheFeatureC;
                wasThreadId2 = Thread.CurrentThread.ManagedThreadId;
                Thread.Sleep(500);
            });
            
            t1.Wait();
            t2.Wait();
            Assert.IsTrue(wasActive1);
            Assert.IsFalse(wasActive2);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadId1);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, wasThreadId2);

        }



    }


}
