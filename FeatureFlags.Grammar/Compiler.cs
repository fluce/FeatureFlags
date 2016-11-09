using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace FeatureFlags.Grammar
{

    public static class Compiler<TGlobals, TReturn>
    {

        private static ConcurrentDictionary<string, Func<TGlobals, TReturn>> cache =
            new ConcurrentDictionary<string, Func<TGlobals, TReturn>>();

        public static Func<TGlobals, TReturn> Compile(string source)
        {
            return cache.GetOrAdd(source, s =>
            {
                Expression expression;
                return Compile(s, out expression);
            });
            ;
        }

        public static Func<TGlobals, TReturn> Compile(string source, out Expression expression)
        {
            RulesGrammarParser p =
                new RulesGrammarParser(
                    new CommonTokenStream(
                        new RulesGrammarLexer(
                            new AntlrInputStream(
                                source)
                            )
                        )
                    )
                {GlobalType = typeof (TGlobals)};

            var listener = new Listener();
            p.AddErrorListener(listener);

            try
            {
                var c = p.compileUnit();

                if (listener.Errors.Count > 0) throw new CompileException(listener.Errors);

                expression = c.expression().ret;

                Expression<Func<TGlobals, TReturn>> ex2;

                try
                {
                    ex2 = Expression.Lambda<Func<TGlobals, TReturn>>(expression,
                        p.GlobalParameter);
                }
                catch (Exception ex)
                {
                    throw new CompileException(ex.Message,innerException: ex);
                }

                var f = ex2.Compile();
                return f;

            }
            catch (SemanticException ex)
            {
                throw new CompileException(ex.Message, innerException: ex);
            }

        }

    }

    public class CompileException : Exception
    {
        public List<CompileException> Errors { get; set; }

        public int Line { get; set; }
        public int Column { get; set; }

        public CompileException(List<CompileException> errors, Exception innerException) : base(errors[0].Message, innerException)
        {
            Errors = errors;
            Line = errors[0].Line;
            Column = errors[0].Column;
        }
        public CompileException(List<CompileException> errors) : base(errors[0].Message, null)
        {
            Errors = errors;
            Line = errors[0].Line;
            Column = errors[0].Column;
        }
        public CompileException(string message, int line=-1, int column=-1, Exception innerException=null) : base(message, innerException)
        {
            Line = line;
            Column = column;
        }

    }

    class Listener : BaseErrorListener
    {

        public List<CompileException>  Errors=new List<CompileException>();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg,
            RecognitionException e)
        {
            Errors.Add(new CompileException($"Syntax error at {line}:{charPositionInLine} : {msg}", line, charPositionInLine, e));
        }

    }

}
