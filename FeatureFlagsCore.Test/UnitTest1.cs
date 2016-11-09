using System.Collections.Specialized;
using NUnit.Framework;

namespace FeatureFlags.Test
{
    [TestFixture]
    public class FeatureFlagGlobal
    {

        IFeatureStore globalFeatureStore;

        [SetUp]
        public void Setup()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureA", "true");
            n.Add("features.FeatureB", "True");
            n.Add("features.FeatureC", "false");
            n.Add("features.FeatureD", "False");
            n.Add("features.FeatureE", "1");
            n.Add("features.FeatureF", "0");
            n.Add("features.FeatureG", "abc");
            n.Add("features.FeatureH", "on");
            n.Add("features.FeatureI", "yes");
            globalFeatureStore = new CachingFeatureStore(new AppSettingsFeatureStore(n));
        }



        [TestCase("true", ExpectedResult = true)]
        [TestCase("True", ExpectedResult = true)]
        [TestCase("false", ExpectedResult = false)]
        [TestCase("False", ExpectedResult = false)]
        [TestCase("1", ExpectedResult = true)]
        [TestCase("0", ExpectedResult = false)]
        [TestCase("abc", ExpectedResult = false)]
        [TestCase("on", ExpectedResult = true)]
        [TestCase("yes", ExpectedResult = true)]
        public bool StaticAppSettingsTest(string value)
        {
            var n = new NameValueCollection {{"features.FeatureA", value}};
            var featureStore = new CachingFeatureStore(new AppSettingsFeatureStore(n));
            return featureStore.GetFeature("FeatureA").IsActive();
        }


        [Test]
        public void DynamicAppSettingsTest()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureA", "true");

            var featureStore = new CachingFeatureStore(new AppSettingsFeatureStore("features.",n,true));

            Assert.IsTrue(featureStore.GetFeature("FeatureA").IsActive());

            n["features.FeatureA"]="false";

            Assert.IsFalse(featureStore.GetFeature("FeatureA").IsActive());

        }

        [Test]
        public void MissingAppSettingsTest()
        {
            Assert.IsFalse(globalFeatureStore.GetFeature("MissingFeature").IsActive());
        }

    }
}
