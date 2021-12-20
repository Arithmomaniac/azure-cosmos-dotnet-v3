﻿namespace StronglyTypedPatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Fasterflect;
    using Microsoft.Azure.Cosmos;

    public class StronglyTypedPatchOperationFactory<TObject>
    {
        private readonly CosmosLinqSerializerOptions serializerOptions;

        internal StronglyTypedPatchOperationFactory(CosmosLinqSerializerOptions serializerOptions)
        {
            this.serializerOptions = serializerOptions;
        }

        public StronglyTypedPatchOperationFactory(CosmosClient client) : this(GetOptions(client))
        {
        }

        private static CosmosLinqSerializerOptions GetOptions(CosmosClient client)
        {
            // see ContainerCore.Items.GetItemLinqQueryable for source of this
            return client.ClientOptions.SerializerOptions is { } options
                ? new() { PropertyNamingPolicy = options.PropertyNamingPolicy }
                : null;
        }

        public PatchOperation Add<TValue>(Expression<Func<TObject, TValue>> path, TValue value)
        {
            return PatchOperation.Add(this.GetJsonPointer(path), value);
        }

        public PatchOperation Remove<TValue>(Expression<Func<TObject, TValue>> path)
        {
            return PatchOperation.Remove(this.GetJsonPointer(path));
        }

        public PatchOperation Replace<TValue>(Expression<Func<TObject, TValue>> path, TValue value)
        {
            return PatchOperation.Replace(this.GetJsonPointer(path), value);
        }

        public PatchOperation Set<TValue>(Expression<Func<TObject, TValue>> path, TValue value)
        {
            return PatchOperation.Set(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, byte>> path, byte value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, short>> path, short value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, int>> path, int value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, long>> path, long value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, sbyte>> path, sbyte value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, ushort>> path, ushort value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, uint>> path, uint value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, ulong>> path, ulong value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, float>> path, float value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, double>> path, double value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), value);
        }

        public PatchOperation Increment(Expression<Func<TObject, decimal>> path, decimal value)
        {
            return PatchOperation.Increment(this.GetJsonPointer(path), (double)value);
        }

        private string GetJsonPointer(LambdaExpression expression)
        {
            //TODO: use expression visitor

            Stack<string> pathParts = new();

            Expression currentExpression = expression.Body;
            while (currentExpression is not ParameterExpression)
            {
                if (currentExpression is MemberExpression memberExpression)
                {
                    if (memberExpression.Member.Name == "Value" && Nullable.GetUnderlyingType(memberExpression.Expression.Type) != null)
                    {
                        //omit nullable .Value calls
                        currentExpression = memberExpression.Expression;
                        continue;
                    }

                    // Member access: fetch serialized name and pop
                    pathParts.Push(GetNameUnderContract(memberExpression.Member));
                    currentExpression = memberExpression.Expression;
                    continue;
                }
                
                if (
                    currentExpression is BinaryExpression binaryExpression and { NodeType: ExpressionType.ArrayIndex }
                )
                {
                    // Array index
                    pathParts.Push(GetIndex(binaryExpression.Right));
                    currentExpression = binaryExpression.Left;
                    continue;
                }
                
                if (
                    currentExpression is MethodCallExpression callExpression and { Arguments: { Count: 1 }, Method: { Name: "get_Item" } }
                )
                {
                    Expression listIndexExpression = callExpression.Arguments[0];

                    if (listIndexExpression.Type == typeof(int))
                    {
                        //// Assume IReadOnlyList index. Int dictionaries are NOT supported.
                        pathParts.Push(GetIndex(listIndexExpression));
                    }
                    else if (listIndexExpression.Type == typeof(string))
                    {
                        
                        //string dictionary. Other dictionary types not supported.
                        pathParts.Push(ResolveConstant<string>(listIndexExpression));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Indexing type (at {currentExpression}) not supported");
                    }    
                    
                    currentExpression = callExpression.Object;
                    continue;
                }

                throw new InvalidOperationException($"{currentExpression.GetType().Name} (at {currentExpression}) not supported");
            }

            return "/" + string.Join("/", pathParts.Select(EscapeJsonPointer));

            string GetNameUnderContract(MemberInfo member)
            {
                Type type = typeof(CosmosClient).Assembly
                    .GetType("Microsoft.Azure.Cosmos.Linq.TypeSystem");

                //TODO: remove Fasterflect; use call delegate instead
                return (string)type.CallMethod("GetMemberName", new[] { typeof(MemberInfo), typeof(CosmosLinqSerializerOptions) }, member, this.serializerOptions);
            } 
            
            string GetIndex(Expression expression)
            {
                int index = ResolveConstant<int>(expression);

                return index switch
                {
                    >= 0 => index.ToString(),
                    -1 => "-", //array append
                    _ => throw new ArgumentOutOfRangeException(nameof(index))
                };
            }

            static string EscapeJsonPointer(string str)
            {
                return new(str.SelectMany(c => c switch
                {
                    '~' => new[] { '~', '0' },
                    '/' => new[] { '~', '1' },
                    _   => new[] { c }
                }).ToArray());
            }
        }

        private static T ResolveConstant<T>(Expression expression)
        {
            Type type = typeof(CosmosClient).Assembly
                                .GetType("Microsoft.Azure.Cosmos.Linq.ConstantEvaluator");

            //TODO: remove Fasterflect; use call delegate instead
            if (type.CallMethod("PartialEval", expression) is not ConstantExpression constantExpression)
            {
                throw new ArgumentException(nameof(expression), "Expression cannot be simplified to a constant");
            }

            return (T)constantExpression.Value;
        }
    }
}
