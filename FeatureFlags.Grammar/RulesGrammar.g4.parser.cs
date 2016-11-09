using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Antlr4.Runtime;

namespace FeatureFlags.Grammar
{
    public class SemanticException : Exception
    {
        public Antlr4.Runtime.ParserRuleContext Context { get; }

        public SemanticException(Antlr4.Runtime.ParserRuleContext context, string message):base(message)
        {
            Context = context;
        }
    }
    

    partial class RulesGrammarParser
    {
        public Type GlobalType
        {
            get { return globalType; }
            set
            {
                globalType = value;
                GlobalParameter = Expression.Parameter(globalType, "global");
            }
        }

        public ParameterExpression GlobalParameter { get; private set; }
        private Type globalType;

        MethodInfo GetMethod(Type type, string methodName, Type[] types)
        {
            return type.GetMethod(methodName, types); 
        }

        Expression GetGlobal()
        {
            return GlobalParameter;
        }

        MethodInfo GetGlobalMethod(string methodName)
        {
            return GlobalType.GetMethod(methodName);
        }

        private Exception handle(ParserRuleContext context, Exception e)
        {
            return
                new CompileException($"Error at {context.Start.Line}:{context.Start.Column} ({context.Start.Text}) : {e.Message}",
                    context.Start.Line,
                    context.Start.Column
                );
        }

        public Expression bin(ParserRuleContext context, ExpressionType type, Expression left, Expression right)
        {
            try
            {
                // handle conversion
                left = c1(left, right);
                right = c2(left, right);

                if (left.Type == typeof (string) && right.Type == typeof (string))
                {
                    switch (type)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                            return Expression.MakeBinary(type,
                                Expression.Call(
                                    typeof(string).GetMethod("Compare",
                                        new[] { typeof(string), typeof(string), typeof(bool), typeof(CultureInfo) }),
                                    left, right, Expression.Constant(false), Expression.Constant(CultureInfo.InvariantCulture)),
                                Expression.Constant(0)
                                );
                        case ExpressionType.Add:
                            return
                                Expression.Call(
                                    typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }),
                                    left,
                                    right
                                );
                        default:
                            return Expression.MakeBinary(type, left, right);
                    }

                }
                return Expression.MakeBinary(type, left, right);
            }
            catch (Exception e)
            {
                throw new CompileException($"Error at {context.Start.Line}:{context.Start.Column} ({context.Start.Text}) : {e.Message}",
                    context.Start.Line,
                    context.Start.Column
                );
            }
        }

        public Expression ternary(ParserRuleContext context, Expression left, ExpressionType type1, Expression middle, ExpressionType type2, Expression right)
        {
            try
            {
                // handle conversion
                var p1 = c1(left, middle);
                var p2 = c2(left, middle);
                var p3 = c2(middle, right);

                var param1 = Expression.Parameter(p1.Type);
                var param2 = Expression.Parameter(p2.Type);
                var param3 = Expression.Parameter(p3.Type);
                return Expression.Block(typeof (bool), new[] {param1, param2, param3},
                    new Expression[]
                    {
                        Expression.Assign(param1, p1),
                        Expression.Assign(param2, p2),
                        Expression.Assign(param3, p3),
                        Expression.And(
                            bin(Context, type1, param1, param2),
                            bin(Context, type2, param2, param3)
                        )
                    }
                );

            }
            catch (Exception e)
            {
                throw new CompileException($"Error at {context.Start.Line}:{context.Start.Column} ({context.Start.Text}) : {e.Message}",
                    context.Start.Line,
                    context.Start.Column
                );
            }
        }

        private Expression c(Expression expression)
        {
            if (expression.Type!=typeof(decimal))
                return Expression.Convert(expression, typeof (decimal));
            return expression;
        }
        private Expression c1(Expression left, Expression right)
        {
            if (left.Type == right.Type)
                return left;
            var cvt=TypeDescriptor.GetConverter(left.Type);
            if (cvt.CanConvertTo(right.Type))
                return Expression.Convert(left, right.Type);
            else
            {
                Tuple<MethodInfo,object> mi;
                if (dico.TryGetValue(new Tuple<Type, Type>(left.Type, right.Type), out mi))
                {
                    return Expression.Call(Expression.Constant(mi.Item2),mi.Item1,left);
                }
                // we will try to convert right to left in c2 function
                return left;
            }
        }
        private Expression c2(Expression left, Expression right)
        {
            // Check if left is convertible to right
            if (left.Type == right.Type)
                return right;
            var cvt = TypeDescriptor.GetConverter(left.Type);
            if (cvt.CanConvertTo(right.Type))
                return right;
            else
            {
                Tuple<MethodInfo, object> mi;
                if (dico.TryGetValue(new Tuple<Type, Type>(left.Type, right.Type), out mi))
                {
                    return right;
                }

                // left is not convertible to right, so we try to convert right to left
                cvt = TypeDescriptor.GetConverter(right.Type);
                if (cvt.CanConvertTo(left.Type))
                    return Expression.Convert(right, left.Type);
                else
                {
                    Tuple<MethodInfo, object> mi2;
                    if (dico.TryGetValue(new Tuple<Type, Type>(right.Type, left.Type), out mi))
                    {
                        return Expression.Call(Expression.Constant(mi.Item2), mi.Item1, right);
                    }


                    throw new Exception($"Cannot convert neither {left.Type.Name} to {right.Type.Name} nor {right.Type.Name} to {left.Type.Name}");
                }
            }
        }

        static RulesGrammarParser()
        {
            //dico.Add(new Tuple<Type, Type>(typeof(DateTime), typeof(TimeSpan)), typeof(Converters).GetMethod("DateTimeToTimeSpan"));
            RegisterConverter<DateTime,TimeSpan>(x=>x.TimeOfDay);
        }

        static void RegisterConverter<From, To>(Func<From, To> convertExpression)
        {
            dico.Add(new Tuple<Type, Type>(typeof(DateTime), typeof(TimeSpan)), new Tuple<MethodInfo,object>(convertExpression.Method, convertExpression.Target));
        }

        static Dictionary<Tuple<Type,Type>, Tuple<MethodInfo, object>> dico=new Dictionary<Tuple<Type, Type>, Tuple<MethodInfo,object>>();

        static class Converters
        {
            public static TimeSpan DateTimeToTimeSpan(DateTime d)
            {
                return d.TimeOfDay;
            }
        }
    }

    public class MyDateTimeConverter : DateTimeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof (TimeSpan)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(TimeSpan)) return ((DateTime)value).TimeOfDay;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
