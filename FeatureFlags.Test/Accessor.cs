using System;
using System.Collections.Specialized;
using FeatureFlags.Stores;
using FeatureFlags.Stores.AppSettings;
using NUnit.Framework;

namespace FeatureFlags.Test
{

    public interface IMyFeatures
    {
        [FeatureFlag("featureA")]
        bool FeatureA { get; }

        bool FeatureB { get; }

        bool FeatureC { get; }
    }

    /*    public class TargetSample : BaseFeatureFlagAccessor, IMyFeatures
        {
            public bool FeatureA { get { return IsActive("featureA"); } }
        }*/

    [TestFixture]
    public class Accessor
    {

        [Test]
        public void BuildAccessor()
        {

            var n = new NameValueCollection();
            n.Add("features.featureA", "true");
            n.Add("features.FeatureB", "true");


            var t = FeatureFlagAccessor.BuildAndInstanciate<IMyFeatures>(
                new Features(new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true)), 
                             new AmbiantContextProvider()));

            Assert.True(t.FeatureA);

            Assert.True(t.FeatureB);

            Assert.False(t.FeatureC);

        }
    }
}