using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using FeatureFlags.Grammar;
using FeatureFlags.Stores;
using FeatureFlags.Stores.AppSettings;
using NUnit.Framework;

namespace FeatureFlags.Test
{
    [TestFixture]
    public class GrammarTest
    {

        [SetUp]
        public void Setup()
        {
            MyGlobals = new Globals()
            {
                b = new Inner1(),
                d = new Inner2()
            };
        }

        Globals MyGlobals;

        public class Globals
        {
            public decimal a { get; set; }
            public decimal x { get; set; }
            public decimal y { get; set; }
            public bool c { get; set; }
            public Inner1 b { get; set; }
            public Inner2 d { get; set; }

            public decimal sin(decimal value)
            {
                return (decimal)Math.Sin((double)value);
            }

            public decimal log(decimal value)
            {
                return (decimal)Math.Log((double)value);
            }

            public DateTime Now => DateTime.Now;

            public DateTime Today1345 => new DateTime(Now.Year, Now.Month, Now.Day, 13, 45, 00);
        }

        public class Inner1
        {
            public bool d(decimal p1, decimal p2)
            {
                return p1 > p2;
            }
        }
        public class Inner2
        {
            public bool h(decimal p1, bool p2)
            {
                return p2 ? p1 > 10 : false;
            }

            public decimal b { get { return 3.14m; } }

            public string st { get; set; }
        }


        class Inner
        {
            public string Prop { get; set; }
        }

        [Test]
        public void GrammarTest0()
        {
            Expression expression;
            var f=Compiler<Globals, bool>.Compile("a>(20*sin(x+y)%5) and (b.d(5,2.5) or not c) and d.b>1.0 and d.h(a,a>x)", out expression);
            Debug.WriteLine(expression.GetDebugView());
            bool res=f(new Globals()
            {
                b = new Inner1(),
                d = new Inner2()
            });
        }

        [Test]
        public void GrammarTest1()
        {
            var f = Compiler<Globals, bool>.Compile("a>(20*sin(x+y)%5) and (b.d(5,2.5) or not c) and d.b>1.0 and d.h(a,a>x)");
        }

        [Test]
        public void GrammarTest2()
        {
            for (int i = 0; i < 1000; i++)
            {
                var f =
                    Compiler<Globals, bool>.Compile(
                        $"a>({i}*sin(x+y)%5) and (b.d(5,2.5) or not c) and d.b>1.0 and d.h(a,a>x)");
            }
        }
        [Test]
        public void GrammarTest3()
        {
            for (int i = 0; i < 1000; i++)
            {
                var f =
                    Compiler<Globals, bool>.Compile(
                        "a>(20*sin(x+y)%5) and (b.d(5,2.5) or not c) and d.b>1.0 and d.h(a,a>x)");
            }
        }

        [TestCase("a and f(x)",true)]
        [TestCase("c and sin(x,y)", true)]
        [TestCase("c and true", false)]
        [TestCase("d.st.Length>10", false)]
        [TestCase(@"d.st.StartsWith(""toto"")", false)]
        public void Parse(string src, bool shouldfail)
        {
            try
            {
                var f = Compiler<Globals, bool>.Compile(src);
                Assert.IsFalse(shouldfail);
            }
            catch (CompileException ex)
            {
                Console.WriteLine(ex.Message);
                if (!shouldfail)
                    throw;
            }
        }

        [TestCase(@"'2016/13/11'", true)]
        [TestCase(@"'2016/02/30'", true)]
        [TestCase(@"'2016/02/05 25:12'", true)]
        [TestCase(@"'2016/02/05 18:63'", true)]
        public void GrammarDates(string src, bool shouldfail)
        {
            try
            {
                var f = Compiler<Globals, DateTime>.Compile(src);
                f(MyGlobals);
            }
            catch (CompileException ex)
            {
                Console.WriteLine(ex.Message);
                if (!shouldfail)
                    throw;
            }
        }

        [Test]
        public void GrammarDateSlash()
        {
            var f = Compiler<Globals, DateTime>.Compile(@"'2016/05/01'");
            Assert.AreEqual(new DateTime(2016,05,01),  f(MyGlobals));
        }

        [Test]
        public void GrammarDateDash()
        {
            var f = Compiler<Globals, DateTime>.Compile(@"'2016-05-01'");
            Assert.AreEqual(new DateTime(2016, 05, 01), f(MyGlobals));
        }

        [Test]
        public void GrammarDateTimeSlash()
        {
            var f = Compiler<Globals, DateTime>.Compile(@"'2016/05/01 18:45'");
            Assert.AreEqual(new DateTime(2016, 05, 01, 18, 45, 0), f(MyGlobals));
        }

        [Test]
        public void GrammarDateTimeDash()
        {
            var f = Compiler<Globals, DateTime>.Compile(@"'2016-05-01 17:12'");
            Assert.AreEqual(new DateTime(2016, 05, 01, 17, 12, 0), f(MyGlobals));
        }

        [TestCase(@"Now>'2016-05-01'", ExpectedResult = true)]
        [TestCase(@"5*2=10", ExpectedResult = true)]
        [TestCase(@"5*2-10=0", ExpectedResult = true)]
        [TestCase(@"25%=1/4", ExpectedResult = true)]
        [TestCase(@"10-5*2=0", ExpectedResult = true)]
        [TestCase(@"Now<'2026-05-01'", ExpectedResult = true)]
        [TestCase(@"'2016-05-01'<Now<'2026-05-01'", ExpectedResult = true)]
        [TestCase(@"'2016-05-01'<Now<'2016-07-01'", ExpectedResult = false)]
        [TestCase(@"Today1345.TimeOfDay<'14:00'", ExpectedResult = true)]
        [TestCase(@"Today1345.TimeOfDay<'13:00'", ExpectedResult = false)]
        [TestCase(@"Today1345<'13:00'", ExpectedResult = false)]
        [TestCase(@"'12:00'<Today1345", ExpectedResult = true)]
        [TestCase(@"'12:00'<Today1345<'16:00'", ExpectedResult = true)]
        [TestCase(@"'13:45'<=Today1345", ExpectedResult = true)]
        [TestCase(@"'13:45'<=Today1345<'16:00'", ExpectedResult = true)]
        [TestCase(@"'13:45'<Today1345", ExpectedResult = false)]
        [TestCase(@"'13:45'<Today1345<'16:00'", ExpectedResult = false)]
        [TestCase(@"'13:45'<'13:00'", ExpectedResult = false)]
        [TestCase(@"""toto"".Length=4", ExpectedResult = true)]
        [TestCase(@"""to""+""to""=""toto""", ExpectedResult = true)]
        [TestCase(@"""a""<""c""", ExpectedResult = true)]
        [TestCase(@"""c""<""a""", ExpectedResult = false)]
        [TestCase(@"""a""<=""a""", ExpectedResult = true)]
        [TestCase(@"""a""<""a""", ExpectedResult = false)]
        [TestCase(@"""a""<=""abcd""<""b""", ExpectedResult = true)]
        public bool Evaluation(string s)
        {
            var f = Compiler<Globals, bool>.Compile(s);
            return f(MyGlobals);
        }

        [TestCase("c and log(-1)>0", typeof(System.OverflowException))]
        [TestCase("a/0>0", typeof(System.DivideByZeroException))]
        public void EvaluationFailure(string src, Type expectedException)
        {
            Assert.Throws(expectedException, () =>
            {
                var f = Compiler<Globals, bool>.Compile(src);
                f(MyGlobals);
            });
        }

    }

    public static class ExpressionExtensions
    {
        public static string GetDebugView(this Expression exp)
        {
            if (exp == null)
                return null;

            var propertyInfo = typeof (Expression).GetProperty("DebugView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return propertyInfo.GetValue(exp) as string;
        }

    }
}
