namespace StronglyTypedPatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using Microsoft.Azure.Cosmos;

    public class JsonPointerBuilder
    {
        // supports finding delegates specified below.
        static readonly Assembly sdkAssembly = typeof(CosmosClient).Assembly;
        static readonly Type cosmosLinqSerializerType = sdkAssembly.GetType("Microsoft.Azure.Cosmos.Linq.DefaultCosmosLinqSerializer");
        static readonly MethodInfo getMemberNameMethod = cosmosLinqSerializerType.GetMethod("SerializeMemberName");

        // optimized invocation of internal methods. See https://mattwarren.org/2016/12/14/Why-is-Reflection-slow/
        // Obviously, the delegates would be replaced by direct access if this was in the main SDK.
        static readonly Func<Expression, Expression> resolveConstantDelegate;
        readonly Func<MemberInfo, string> getMemberNameDelegate;

        static JsonPointerBuilder()
        {
            MethodInfo resolveConstantMethod = sdkAssembly
                .GetType("Microsoft.Azure.Cosmos.Linq.ConstantEvaluator")
                .GetMethod("PartialEval", new[] {typeof(Expression) });
            resolveConstantDelegate = (Func<Expression, Expression>)Delegate.CreateDelegate(
                typeof(Func<Expression, Expression>),
                resolveConstantMethod);
        }

        internal JsonPointerBuilder(CosmosLinqSerializerOptions serializerOptions)
        {
            object defaultSerializerInstance = Activator.CreateInstance(cosmosLinqSerializerType, serializerOptions?.PropertyNamingPolicy ?? default);
            this.getMemberNameDelegate = (Func<MemberInfo, string>)Delegate.CreateDelegate(
                typeof(Func<MemberInfo, string>),
                defaultSerializerInstance,
                getMemberNameMethod);
        }

        public string Build(LambdaExpression expression)
        {
            Stack<string> pathParts = new();

            Expression currentExpression = expression.Body;
            while (currentExpression is not ParameterExpression)
            {
                currentExpression = this.GrabSegmentAndTraverse(currentExpression, out string pathPart);
                if (pathPart != null)
                {
                    pathParts.Push(pathPart);
                }
            }

            return pathParts.Aggregate(new StringBuilder(), AddEscapedPathPart).ToString();

            static StringBuilder AddEscapedPathPart(StringBuilder sb, string str)
            {
                return str.Aggregate(
                    sb.Append('/'), //each part starts with a /
                    (sb, c) => c switch //escape string per JsonPointer spec
                    {
                        '~' => sb.Append('~').Append('0'),
                        '/' => sb.Append('~').Append('1'),
                        _ => sb.Append(c),
                    });
            }
        }

        private Expression GrabSegmentAndTraverse(Expression currentExpression, out string pathPart)
        {
            pathPart = null;

            if (currentExpression is MemberExpression memberExpression)
            {
                if (memberExpression.Member.Name == "Value" && Nullable.GetUnderlyingType(memberExpression.Expression.Type) != null)
                {
                    //omit nullable .Value calls
                    return memberExpression.Expression;
                }

                // Member access: fetch serialized name and pop
                pathPart = this.getMemberNameDelegate(memberExpression.Member);
                return memberExpression.Expression;
            }

            if (currentExpression is BinaryExpression { NodeType: ExpressionType.ArrayIndex } binaryExpression)
            {
                // Array index
                pathPart = GetIndex(binaryExpression.Right);
                return binaryExpression.Left;
            }

            if (currentExpression is MethodCallExpression { Arguments: { Count: 1 }, Method: { Name: "get_Item" } } callExpression)
            {
                Expression listIndexExpression = callExpression.Arguments[0];

                if (listIndexExpression.Type == typeof(int))
                {
                    //// Assume IReadOnlyList index. Int dictionaries are NOT supported.
                    pathPart = GetIndex(listIndexExpression);
                }
                else if (listIndexExpression.Type == typeof(string))
                {

                    //string dictionary. Other dictionary types not supported.
                    pathPart = ResolveConstant<string>(listIndexExpression);
                }
                else
                {
                    throw new InvalidOperationException($"Indexing type (at {currentExpression}) not supported");
                }

                return callExpression.Object;
            }

            throw new InvalidOperationException($"{currentExpression.GetType().Name} (at {currentExpression}) not supported");
        }

        private static string GetIndex(Expression expression)
        {
            int index = ResolveConstant<int>(expression);

            return index switch
            {
                >= 0 => index.ToString(),
                -1 => "-", //array append
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }       

        private static T ResolveConstant<T>(Expression expression)
        {
            if (resolveConstantDelegate(expression) is not ConstantExpression constantExpression)
            {
                throw new ArgumentException(nameof(expression), "Expression cannot be simplified to a constant");
            }

            return (T)constantExpression.Value;
        }
    }
}
