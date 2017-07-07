using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FormatExpressionTest
{
    class Program
    {
        public delegate string FormatDelegate(string str, params object[] vals);

        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < 10000; i++)
            {
                var fieldMem = "my field asdf";

                var t0 = Test(() => $"const int: {12345}, const str: {"mystring"}, field {fieldMem}, prop {PropTest}, method {ReturnVal()}");
                var t1 = Test(() => string.Format("test my {0}, vallll {1}", fieldMem, 12345, "asdf", 222));
                var t2 = Test(() => $"test my {12345}, vallll {"asdf"} next {222} and prop {333}");
                var t3 = Test(() => $"test my {1234}");
                var t4 = Test(() => $"test my {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}");
            }

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine(elapsed);
            Console.ReadLine();
        }

        static string ReturnVal()
        {
            return "mymethodreturnval";
        }

        static string PropTest => "MyProp";

        static (string Query, object[] Params) Test(Expression<Func<string>> expression)
        {
            var methodCallExp = (MethodCallExpression)expression.Body;
            if (methodCallExp.Arguments[0] is MethodCallExpression n)
            {
                methodCallExp = n;
            }

            var formatStr = (string)((ConstantExpression)methodCallExp.Arguments[0]).Value;

            Expression[] args = methodCallExp.Arguments.ToArray();
            if (args.Length > 1 && args[1] is NewArrayExpression newArrExp)
            {
                args = newArrExp.Expressions.ToArray();
            }
            else
            {
                args = methodCallExp.Arguments.Skip(1).ToArray();
            }

            var paramNames = new string[args.Length];
            var paramVals = new object[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                paramNames[i] = "@p" + i;
                var arg = args[i];
                paramVals[i] = GetExpressionValue(arg);
            }

            var queryStr = string.Format(formatStr, paramNames);
            return (queryStr, paramVals);
        }

        static object GetExpressionValue(Expression exp)
        {
            switch (exp)
            {
                case ConstantExpression constExp:
                    return constExp.Value;
                case UnaryExpression urnaryExp:
                    return ((ConstantExpression)urnaryExp.Operand).Value;
                case MemberExpression memberExp:
                    switch (memberExp.Member)
                    {
                        case FieldInfo fieldInfo:
                            return fieldInfo.GetValue(((ConstantExpression)memberExp.Expression).Value);
                        case PropertyInfo propInfo: 
                            //return propInfo.GetValue(((ConstantExpression)memberExp.Expression).Value);
                        default:
                            //throw new ArgumentException("unknown member expression type");
                            return Expression.Lambda(exp).Compile().DynamicInvoke();
                    }
                default:
                    //throw new ArgumentException("Uknown expression type");
                    return Expression.Lambda(exp).Compile().DynamicInvoke();
            }
        }
    }
}
