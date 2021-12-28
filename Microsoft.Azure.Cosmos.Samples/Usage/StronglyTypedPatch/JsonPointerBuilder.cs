﻿namespace StronglyTypedPatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Fasterflect;
    using Microsoft.Azure.Cosmos;

    public class JsonPointerBuilder
    {
        private readonly CosmosLinqSerializerOptions serializerOptions;

        internal JsonPointerBuilder(CosmosLinqSerializerOptions serializerOptions)
        {
            this.serializerOptions = serializerOptions;
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

            return "/" + string.Join("/", pathParts.Select(EscapeJsonPointer));
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
                pathPart = this.GetNameUnderContract(memberExpression.Member);
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

        private string GetNameUnderContract(MemberInfo member)
        {
            Type type = typeof(CosmosClient).Assembly.GetType("Microsoft.Azure.Cosmos.Linq.TypeSystem");

            //TODO: remove Fasterflect; use call delegate instead
            return (string)type.CallMethod("GetMemberName", new[] { typeof(MemberInfo), typeof(CosmosLinqSerializerOptions) }, member, this.serializerOptions);
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

        private static string EscapeJsonPointer(string str)
        {
            return new(str.SelectMany(c => c switch
            {
                '~' => new[] { '~', '0' },
                '/' => new[] { '~', '1' },
                _ => new[] { c }
            }).ToArray());
        }

        private static T ResolveConstant<T>(Expression expression)
        {
            Type type = typeof(CosmosClient).Assembly.GetType("Microsoft.Azure.Cosmos.Linq.ConstantEvaluator");

            //TODO: remove Fasterflect; use call delegate instead
            if (type.CallMethod("PartialEval", expression) is not ConstantExpression constantExpression)
            {
                throw new ArgumentException(nameof(expression), "Expression cannot be simplified to a constant");
            }

            return (T)constantExpression.Value;
        }
    }
}