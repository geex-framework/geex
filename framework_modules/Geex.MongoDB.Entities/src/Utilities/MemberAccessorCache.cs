using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

using MongoDB.Bson.Serialization;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 基于表达式编译的成员访问器实现
    /// </summary>
    public class MemberAccessor
    {
        private readonly Func<object, object> _getter;
        private readonly Action<object, object> _setter;

        public MemberAccessor(MemberInfo memberInfo)
        {
            _getter = CreateGetter(memberInfo);
            _setter = CreateSetter(memberInfo);
        }

        public object GetValue(object obj) => _getter(obj);
        public void SetValue(object obj, object value) => _setter(obj, value);

        private static Func<object, object> CreateGetter(MemberInfo memberInfo)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var objCast = Expression.Convert(objParam, memberInfo.DeclaringType);

            Expression memberAccess = memberInfo switch
            {
                PropertyInfo property => Expression.Property(objCast, property),
                FieldInfo field => Expression.Field(objCast, field),
                _ => throw new ArgumentException($"Unsupported member type: {memberInfo.GetType()}")
            };

            var resultCast = Expression.Convert(memberAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(resultCast, objParam);

            return lambda.CompileFast();
        }

        private static Action<object, object> CreateSetter(MemberInfo memberInfo)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var objCast = Expression.Convert(objParam, memberInfo.DeclaringType);

            Type memberType = memberInfo switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => throw new ArgumentException($"Unsupported member type: {memberInfo.GetType()}")
            };

            var valueCast = Expression.Convert(valueParam, memberType);

            Expression memberAccess = memberInfo switch
            {
                PropertyInfo property when property.CanWrite => Expression.Property(objCast, property),
                FieldInfo field => Expression.Field(objCast, field),
                _ => null
            };

            if (memberAccess == null)
                return (obj, value) => { }; // Read-only member, do nothing

            var assignment = Expression.Assign(memberAccess, valueCast);
            var lambda = Expression.Lambda<Action<object, object>>(assignment, objParam, valueParam);

            return lambda.CompileFast();
        }
    }

    /// <summary>
    /// 成员访问器缓存，提供高性能的成员访问
    /// </summary>
    public static class MemberAccessorCache
    {
        private static readonly ConcurrentDictionary<MemberInfo, MemberAccessor> _accessorCache
            = new ConcurrentDictionary<MemberInfo, MemberAccessor>();

        /// <summary>
        /// 获取或创建成员访问器
        /// </summary>
        public static MemberAccessor GetAccessor(MemberInfo memberInfo)
        {
            return _accessorCache.GetOrAdd(memberInfo, info => new MemberAccessor(info));
        }

        /// <summary>
        /// 获取或创建成员访问器（通过BsonMemberMap）
        /// </summary>
        public static MemberAccessor GetAccessor(BsonMemberMap memberMap)
        {
            return GetAccessor(memberMap.MemberInfo);
        }

        /// <summary>
        /// 高性能获取成员值
        /// </summary>
        public static object GetValue(object obj, BsonMemberMap memberMap)
        {
            var accessor = GetAccessor(memberMap);
            return accessor.GetValue(obj);
        }

        /// <summary>
        /// 高性能设置成员值
        /// </summary>
        public static void SetValue(object obj, BsonMemberMap memberMap, object value)
        {
            var accessor = GetAccessor(memberMap);
            accessor.SetValue(obj, value);
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _accessorCache.Clear();
        }
    }
}
