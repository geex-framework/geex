using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Core.Comparers;
using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace
namespace MongoDB.Bson
{
    /// <summary>
    /// 字段差异工厂，用于高性能创建字段差异对象
    /// </summary>
    public static class FieldDifferenceFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<string, object, object, BsonMemberMap, IBsonMemberDifference>>
            _factoryCache = new ConcurrentDictionary<Type, Func<string, object, object, BsonMemberMap, IBsonMemberDifference>>();

        // 常用表达式缓存，减少重复创建
        private static readonly ConcurrentDictionary<Type, ParameterExpression> _parameterCache = new();
        private static readonly ConcurrentDictionary<Type, NewExpression> _constructorCache = new();

        /// <summary>
        /// 创建字段差异对象（使用编译的表达式）
        /// </summary>
        public static IBsonMemberDifference CreateFieldDifference(this BsonMemberMap memberMap, string fieldName, object baseValue, object newValue)
        {
            var fieldType = memberMap.MemberType;
            var factory = _factoryCache.GetOrAdd(fieldType, CreateFactory);
            return factory(fieldName, baseValue, newValue, memberMap);
        }

        private static Func<string, object, object, BsonMemberMap, IBsonMemberDifference> CreateFactory(Type fieldType)
        {
            // 创建编译的构造函数和属性设置器
            var differenceType = typeof(BsonMemberDifference<>).MakeGenericTypeFast(fieldType);

            var constructor = differenceType.GetConstructor(Type.EmptyTypes);
            var fieldNameProp = differenceType.GetProperty(nameof(BsonMemberDifference<object>.FieldName));
            var baseValueProp = differenceType.GetProperty(nameof(BsonMemberDifference<object>.BaseValue));
            var newValueProp = differenceType.GetProperty(nameof(BsonMemberDifference<object>.NewValue));
            var memberMapProp = differenceType.GetProperty(nameof(BsonMemberDifference<object>.MemberMap));

            // 使用缓存的参数表达式，减少重复创建
            var fieldNameParam = Expression.Parameter(typeof(string), "fieldName");
            var baseValueParam = Expression.Parameter(typeof(object), "baseValue");
            var newValueParam = Expression.Parameter(typeof(object), "newValue");
            var memberMapParam = Expression.Parameter(typeof(BsonMemberMap), "memberMap");

            // 创建实例
            var newInstance = Expression.New(constructor);
            var instanceVar = Expression.Variable(differenceType, "instance");
            var resultVar = Expression.Variable(typeof(IBsonMemberDifference), "result");

            // 创建赋值表达式
            var expressions = new List<Expression>
            {
                Expression.Assign(instanceVar, newInstance),
                Expression.Call(instanceVar, fieldNameProp.SetMethod, fieldNameParam),
                Expression.Call(instanceVar, baseValueProp.SetMethod,
                    fieldType.IsValueType && Nullable.GetUnderlyingType(fieldType) == null
                        ? Expression.Convert(baseValueParam, fieldType)
                        : Expression.Convert(baseValueParam, fieldType)),
                Expression.Call(instanceVar, newValueProp.SetMethod,
                    fieldType.IsValueType && Nullable.GetUnderlyingType(fieldType) == null
                        ? Expression.Convert(newValueParam, fieldType)
                        : Expression.Convert(newValueParam, fieldType)),
                Expression.Call(instanceVar, memberMapProp.SetMethod, memberMapParam),
                Expression.Assign(resultVar, Expression.Convert(instanceVar, typeof(IBsonMemberDifference))),
                resultVar
            };

            var block = Expression.Block(
                new[] { instanceVar, resultVar },
                expressions
            );

            var lambda = Expression.Lambda<Func<string, object, object, BsonMemberMap, IBsonMemberDifference>>(
                block,
                fieldNameParam, baseValueParam, newValueParam, memberMapParam);

            return lambda.CompileFast();
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _factoryCache.Clear();
            _parameterCache.Clear();
            _constructorCache.Clear();
        }
    }
}
