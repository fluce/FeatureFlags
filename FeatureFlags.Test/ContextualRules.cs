using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeatureFlags.Evaluator;
using FeatureFlags.Stores;
using FeatureFlags.Stores.AppSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace FeatureFlags.Test
{
    [TestFixture]
    public class ContextualRules
    {

        [TestCase("true", ExpectedResult = true)]
        [TestCase("false", ExpectedResult = false)]
        [TestCase("'2016-11-01 14:00'<Now<'2016-11-01 15:00'", ExpectedResult = true)]
        [TestCase("'14:00'<Now<'15:00'", ExpectedResult = true)]
        [TestCase("'2016-10-01 14:00'<Now<'2016-10-01 15:00'", ExpectedResult = false)]
        [TestCase("'15:00'<Now<'16:00'", ExpectedResult = false)]
        public bool AsAnonymous(string expression)
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureC", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = expression}));

            var features = new Features(new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true)),
                new TestContextProvider(new FeatureContext {DateTime = new DateTime(2016, 11, 01, 14, 30, 00)}));

            return features.IsActive("FeatureC");
        }

        [TestCase("User.Sample(25%)", "", "5F8FD26F-C23C-4744-8499-FFFFFFFF0000", ExpectedResult = true)]
        [TestCase("User.Sample(25%)", "", "5F8FD26F-C23C-4744-8499-FFFFFFFF3FFF", ExpectedResult = true)]
        [TestCase("User.Sample(25%)", "", "5F8FD26F-C23C-4744-8499-FFFFFFFF4000", ExpectedResult = false)]
        [TestCase("User.Sample(25%)", "", "5F8FD26F-C23C-4744-8499-FFFFFFFF4001", ExpectedResult = false)]
        [TestCase("User.Sample(25%)", "", "5F8FD26F-C23C-4744-8499-FFFFFFFFFFFF", ExpectedResult = false)]
        [TestCase("User.Email=\"test@domain.com\"", "test@domain.com", null, ExpectedResult = true)]
        [TestCase("User.Email=\"test@domain.com\"", "othertest@domain.com", null, ExpectedResult = false)]
        [TestCase("User.Email.EndsWith(\"@domain.com\")", "test@domain.com", null, ExpectedResult = true)]
        [TestCase("User.Email.EndsWith(\"@domain.com\")", "test@otherdomain.com", null, ExpectedResult = false)]
        [TestCase("'14:00'<Now<'15:00' and User.Sample(25%) and User.Email.EndsWith(\"@domain.com\")", "test@domain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF3FFF", ExpectedResult = true)]
        [TestCase("'12:00'<Now<'13:00' and User.Sample(25%) and User.Email.EndsWith(\"@domain.com\")", "test@domain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF3FFF", ExpectedResult = false)]
        [TestCase("'14:00'<Now<'15:00' and User.Sample(25%) and User.Email.EndsWith(\"@domain.com\")", "test@otherdomain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF3FFF", ExpectedResult = false)]
        [TestCase("'14:00'<Now<'15:00' and User.Sample(25%) and User.Email.EndsWith(\"@domain.com\")", "test@domain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF8FFF", ExpectedResult = false)]
        [TestCase("'12:00'<Now<'13:00' or User.Sample(25%) or User.Email.EndsWith(\"@domain.com\")", "test@domain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF8FFF", ExpectedResult = true)]
        [TestCase("'14:00'<Now<'15:00' or User.Sample(25%) or User.Email.EndsWith(\"@domain.com\")", "test@otherdomain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF8FFF", ExpectedResult = true)]
        [TestCase("'12:00'<Now<'13:00' or User.Sample(25%) or User.Email.EndsWith(\"@domain.com\")", "test@otherdomain.com", "5F8FD26F-C23C-4744-8499-FFFFFFFF3FFF", ExpectedResult = true)]
        public bool AsUser (string expression, string email, string guid)
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureC", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = expression }));

            var features = new Features(new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true)),
                new TestContextProvider(
                    new FeatureContext
                    {
                        DateTime = new DateTime(2016, 11, 01, 14, 30, 00),
                        Uid = guid!=null?(Guid?)Guid.Parse(guid):null,
                        Email = email
                    }));

            return features.IsActive("FeatureC");
        }

        [Test]
        public void RecursiveRule()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureC", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = "IsActive(\"FeatureA\")" }));
            n.Add("features.FeatureA", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = "true" }));

            var features = new Features(new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true)),
                new TestContextProvider(
                    new FeatureContext
                    {
                        DateTime = new DateTime(2016, 11, 01, 14, 30, 00),
                        Uid = null,
                        Email = null
                    }));

            Assert.IsTrue(features.IsActive("FeatureC"));
        }

        [Test]
        public void RecursiveRuleWithLoop1()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureC", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = "IsActive(\"FeatureC\")" }));
            n.Add("features.FeatureA", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = "true" }));

            var features = new Features(new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true)),
                new TestContextProvider(
                    new FeatureContext
                    {
                        DateTime = new DateTime(2016, 11, 01, 14, 30, 00),
                        Uid = null,
                        Email = null
                    }));

            Assert.IsFalse(features.IsActive("FeatureC"));
        }

        [Test]
        public void RecursiveRuleWithLoop2()
        {
            var n = new NameValueCollection();
            n.Add("features.FeatureC", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = "IsActive(\"FeatureA\")" }));
            n.Add("features.FeatureA", JsonConvert.SerializeObject(new FeatureRulesDefinition { ActiveExpression = "IsActive(\"FeatureC\")" }));

            var features = new Features(new CachingFeatureStore(new AppSettingsFeatureStore("features.", n, true)),
                new TestContextProvider(
                    new FeatureContext
                    {
                        DateTime = new DateTime(2016, 11, 01, 14, 30, 00),
                        Uid = null,
                        Email = null
                    }));

            Assert.IsFalse(features.IsActive("FeatureC"));
        }

    }

    public class TestContextProvider: IFeatureContextProvider
    {
        public FeatureContext Context { get; set; }

        public TestContextProvider(FeatureContext context)
        {
            Context = context;
        }

        public FeatureContext GetContext()
        {
            return Context;
        }
    }


}
