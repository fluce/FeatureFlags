using System;
using System.Collections.Specialized;
using System.Globalization;
using FeatureFlags.Rules;
using NUnit.Framework;

namespace FeatureFlags.Test
{
    [TestFixture]
    public class FeatureFlagParser
    {
        
        [TestCase("true", ExpectedResult = true)]
        [TestCase("True", ExpectedResult = true)]
        [TestCase("false", ExpectedResult = false)]
        [TestCase("False", ExpectedResult = false)]
        [TestCase("1", ExpectedResult = true)]
        [TestCase("0", ExpectedResult = false)]
        [TestCase("abc", ExpectedResult = false)]
        [TestCase("on", ExpectedResult = true)]
        [TestCase("yes", ExpectedResult = true)]
        public bool ParserConstant(string value)
        {
            var a = FeatureFlagUtils.Parse(value);
            Assert.IsInstanceOf<ConstantFeatureFlagStateEvaluator>(a);
            return a.Evaluate(null)==FeatureFlagState.Active;
        }


        [Test]
        public void ComplexValue()
        {
            var value = @"
{
    'contextualRules': [
        { 
            'rule': 'Or',
            'rules': [ 
                {
                    'rule': 'Schedule',
                    'from': '2016-11-28T12:00:00', 
                    'to': '2016-11-28T14:00:00'
                },
                {
                    'rule': 'Schedule',
                    'from': '2016-11-28T18:00:00', 
                    'to': '2016-11-28T20:00:00'
                },
            ]
        },
        {
            'rule': 'Or',
            'rules': [ 
                {
                    'rule': 'UserSelection',
                    'uid': 'DF767C54-AB7F-4158-ADBC-56C38B4E7831',
                },
                {
                    'rule': 'UserSelection',
                    'ratio': '0.05'
                },
            ]
        }
    ]
}
";
            var a = FeatureFlagUtils.Parse(value);

            Assert.IsInstanceOf<DynamicFeatureFlagStateEvaluator>(a);

            var ev = (DynamicFeatureFlagStateEvaluator)a;
            Assert.AreEqual(2, ev.Rules.ContextualRules.Length);
            Assert.IsInstanceOf<OrRule>(ev.Rules.ContextualRules[0]);
            Assert.AreEqual(2, ((OrRule)ev.Rules.ContextualRules[0]).Rules.Length);
            Assert.IsInstanceOf<ScheduleRule>(((OrRule)ev.Rules.ContextualRules[0]).Rules[0]);
            Assert.IsInstanceOf<ScheduleRule>(((OrRule)ev.Rules.ContextualRules[0]).Rules[1]);
            Assert.IsInstanceOf<OrRule>(ev.Rules.ContextualRules[1]);
            Assert.AreEqual(2, ((OrRule)ev.Rules.ContextualRules[1]).Rules.Length);
            Assert.IsInstanceOf<UserSelectionRule>(((OrRule)ev.Rules.ContextualRules[1]).Rules[0]);
            Assert.IsInstanceOf<UserSelectionRule>(((OrRule)ev.Rules.ContextualRules[1]).Rules[1]);
        }

        [Test]
        public void CheckRuleType()
        {
            Rule r=new OrRule();
            Assert.AreEqual("Or", r.RuleType);
            r = new ScheduleRule();
            Assert.AreEqual("Schedule", r.RuleType);
            r = new UserSelectionRule();
            Assert.AreEqual("UserSelection", r.RuleType);
        }

        [TestCase(FeatureFlagState.Active, FeatureFlagState.Inactive, ExpectedResult = FeatureFlagState.Active)]
        [TestCase(FeatureFlagState.Active, FeatureFlagState.Active, ExpectedResult = FeatureFlagState.Active)]
        [TestCase(FeatureFlagState.Inactive, FeatureFlagState.Inactive, ExpectedResult = FeatureFlagState.Inactive)]
        public FeatureFlagState OrRule(FeatureFlagState p1, FeatureFlagState p2)
        {
            var or = new OrRule {Rules = new Rule[] {new ConstantRule {Value=p1}, new ConstantRule { Value=p2} }};
            return or.Evaluate(null);
        }

        [TestCase(FeatureFlagState.Active, FeatureFlagState.Inactive, ExpectedResult = FeatureFlagState.Inactive)]
        [TestCase(FeatureFlagState.Active, FeatureFlagState.Active, ExpectedResult = FeatureFlagState.Active)]
        [TestCase(FeatureFlagState.Inactive, FeatureFlagState.Inactive, ExpectedResult = FeatureFlagState.Inactive)]
        public FeatureFlagState AndRule(FeatureFlagState p1, FeatureFlagState p2)
        {
            var or = new AndRule { Rules = new Rule[] { new ConstantRule { Value = p1 }, new ConstantRule { Value = p2 } } };
            return or.Evaluate(null);
        }

        [TestCase("2016/06/01 15:00", "2016/06/01 18:00", "2016/06/01 14:00", ExpectedResult = FeatureFlagState.Inactive)]
        [TestCase("2016/06/01 15:00", "2016/06/01 18:00", "2016/06/01 15:00", ExpectedResult = FeatureFlagState.Active)]
        [TestCase("2016/06/01 15:00", "2016/06/01 18:00", "2016/06/01 16:00", ExpectedResult = FeatureFlagState.Active)]
        [TestCase("2016/06/01 15:00", "2016/06/01 18:00", "2016/06/01 18:00", ExpectedResult = FeatureFlagState.Inactive)]
        [TestCase("2016/06/01 15:00", "2016/06/01 18:00", "2016/06/01 19:00", ExpectedResult = FeatureFlagState.Inactive)]
        public FeatureFlagState ScheduleRule(string p1, string p2, string currentDateTime)
        {
            var or = new ScheduleRule { From = DateTime.ParseExact(p1,"yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                                        To = DateTime.ParseExact(p2, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture) };
            return or.Evaluate(new FeatureContext { DateTime = DateTime.ParseExact(currentDateTime, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture) });
        }

        [TestCase("4A8AC574-90EF-47A3-95B8-19A7582DC95C", null, "4A8AC574-90EF-47A3-95B8-19A7582DC95C", ExpectedResult = FeatureFlagState.Active)]
        [TestCase("4A8AC574-90EF-47A3-95B8-19A7582DC95C", null, "5F8FD26F-C23C-4744-8499-9939EC9B3CC5", ExpectedResult = FeatureFlagState.Inactive)]
        [TestCase(null, 0.25, "5F8FD26F-C23C-4744-8499-FFFFFFFF0000", ExpectedResult = FeatureFlagState.Active)]
        [TestCase(null, 0.25, "5F8FD26F-C23C-4744-8499-FFFFFFFFFFFF", ExpectedResult = FeatureFlagState.Inactive)]
        [TestCase(null, 0.25, "5F8FD26F-C23C-4744-8499-FFFFFFFF3FFF", ExpectedResult = FeatureFlagState.Active)]
        [TestCase(null, 0.25, "5F8FD26F-C23C-4744-8499-FFFFFFFF4000", ExpectedResult = FeatureFlagState.Inactive)]
        public FeatureFlagState UserRule(string uid, decimal? ratio, string user)
        {
            var ur = new UserSelectionRule
            {
                Uid = uid==null?(Guid?)null:Guid.Parse(uid),
                Ratio = ratio
            };
            return
                ur.Evaluate(new FeatureContext
                {
                    Uid = Guid.Parse(user)
                });
        }


    }

}
