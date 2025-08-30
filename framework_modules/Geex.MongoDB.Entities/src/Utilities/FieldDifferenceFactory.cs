using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Core.Comparers;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 字段差异工厂，用于高性能创建字段差异对象
    /// </summary>
    public static class FieldDifferenceFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<string, object, object, BsonMemberMap, IBsonFieldDifference>>
            _factoryCache = new ConcurrentDictionary<Type, Func<string, object, object, BsonMemberMap, IBsonFieldDifference>>();

        /// <summary>
        /// 创建字段差异对象（使用编译的表达式）
        /// </summary>
        public static IBsonFieldDifference CreateFieldDifference(string fieldName, object baseValue, object newValue, BsonMemberMap memberMap)
        {
            var fieldType = memberMap.MemberType;
            var factory = _factoryCache.GetOrAdd(fieldType, CreateFactory);
            return factory(fieldName, baseValue, newValue, memberMap);
        }

        private static Func<string, object, object, BsonMemberMap, IBsonFieldDifference> CreateFactory(Type fieldType)
        {
            // 创建编译的构造函数和属性设置器
            var differenceType = typeof(BsonFieldDifference<>).MakeGenericType(fieldType);

            var constructor = differenceType.GetConstructor(Type.EmptyTypes);
            var fieldNameProp = differenceType.GetProperty(nameof(BsonFieldDifference<object>.FieldName));
            var baseValueProp = differenceType.GetProperty(nameof(BsonFieldDifference<object>.BaseValue));
            var newValueProp = differenceType.GetProperty(nameof(BsonFieldDifference<object>.NewValue));
            var memberMapProp = differenceType.GetProperty(nameof(BsonFieldDifference<object>.MemberMap));
            var hasBaseValueProp = differenceType.GetProperty(nameof(BsonFieldDifference<object>.HasBaseValue));
            var hasNewValueProp = differenceType.GetProperty(nameof(BsonFieldDifference<object>.HasNewValue));

            // 创建参数表达式
            var fieldNameParam = Expression.Parameter(typeof(string), "fieldName");
            var baseValueParam = Expression.Parameter(typeof(object), "baseValue");
            var newValueParam = Expression.Parameter(typeof(object), "newValue");
            var memberMapParam = Expression.Parameter(typeof(BsonMemberMap), "memberMap");

            // 创建实例
            var newInstance = Expression.New(constructor);
            var instanceVar = Expression.Variable(differenceType, "instance");
            var resultVar = Expression.Variable(typeof(IBsonFieldDifference), "result");

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
                Expression.Call(instanceVar, hasBaseValueProp.SetMethod, Expression.Constant(true)),
                Expression.Call(instanceVar, hasNewValueProp.SetMethod, Expression.Constant(true)),
                Expression.Assign(resultVar, Expression.Convert(instanceVar, typeof(IBsonFieldDifference))),
                resultVar
            };

            var block = Expression.Block(
                new[] { instanceVar, resultVar },
                expressions
            );

            var lambda = Expression.Lambda<Func<string, object, object, BsonMemberMap, IBsonFieldDifference>>(
                block,
                fieldNameParam, baseValueParam, newValueParam, memberMapParam);

            try
            {
                return lambda.CompileFast();
            }
            catch (Exception)
            {
                // 如果编译失败，回退到反射方式
                return CreateReflectionBasedFactory(fieldType);
            }
        }

        private static Func<string, object, object, BsonMemberMap, IBsonFieldDifference> CreateReflectionBasedFactory(Type fieldType)
        {
            return (fieldName, baseValue, newValue, memberMap) =>
            {
                var differenceType = typeof(BsonFieldDifference<>).MakeGenericType(fieldType);
                var difference = (IBsonFieldDifference)Activator.CreateInstance(differenceType);

                var fieldNameProperty = differenceType.GetProperty(nameof(BsonFieldDifference<object>.FieldName));
                var baseValueProperty = differenceType.GetProperty(nameof(BsonFieldDifference<object>.BaseValue));
                var newValueProperty = differenceType.GetProperty(nameof(BsonFieldDifference<object>.NewValue));
                var memberMapProperty = differenceType.GetProperty(nameof(BsonFieldDifference<object>.MemberMap));

                fieldNameProperty?.SetValue(difference, fieldName);
                baseValueProperty?.SetValue(difference, baseValue);
                newValueProperty?.SetValue(difference, newValue);
                memberMapProperty?.SetValue(difference, memberMap);

                return difference;
            };
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _factoryCache.Clear();
        }
    }
}
