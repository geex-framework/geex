using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

using Geex;
using Geex.MongoDB.Entities.Utilities;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Utilities;

using static FastExpressionCompiler.ExpressionCompiler;

namespace MongoDB.Entities.Core.Comparers
{
    /// <summary>
    /// 比较结果类型
    /// </summary>
    public enum BsonDiffMode
    {
        /// <summary>
        /// 快速模式：找到第一个差异就停止
        /// </summary>
        Fast,
        /// <summary>
        /// 完整模式：收集所有顶层字段差异
        /// </summary>
        Full
    }

    /// <summary>
    /// 字段差异接口，支持类型安全的字段访问
    /// </summary>
    public interface IBsonMemberDifference
    {
        string FieldName { get; }
        Type FieldType { get; }
        BsonMemberMap MemberMap { get; }
        /// <summary>
        /// 将字段值应用到目标对象
        /// </summary>
        void ApplyBaseValue(object target);
        void ApplyNewValue(object target);
        object BaseValue { get; }
        object NewValue { get; }
    }

    public class BsonMemberDifference : IBsonMemberDifference
    {
        /// <inheritdoc />
        public string FieldName { get; internal set; }

        /// <inheritdoc />
        public Type FieldType { get; internal set; }

        /// <inheritdoc />
        public BsonMemberMap MemberMap { get; internal set; }

        /// <inheritdoc />
        public void ApplyBaseValue(object target)
        {
            MemberMap.Setter(target, BaseValue);
        }

        /// <inheritdoc />
        public void ApplyNewValue(object target)
        {
            MemberMap.Setter(target, NewValue);
        }

        /// <inheritdoc />
        public object BaseValue { get; internal set; }

        /// <inheritdoc />
        public object NewValue { get; internal set; }
    }
    /// <summary>
    /// 泛型字段差异信息，避免装拆箱操作
    /// </summary>
    public class BsonMemberDifference<TField> : IBsonMemberDifference
    {
        object IBsonMemberDifference.NewValue => NewValue;
        object IBsonMemberDifference.BaseValue => BaseValue;
        public string FieldName { get; set; }
        public TField BaseValue { get; set; }
        public TField NewValue { get; set; }
        public BsonMemberMap MemberMap { get; set; }

        public Type FieldType => typeof(TField);

        public void ApplyBaseValue(object target)
        {
            MemberMap.Setter(target, BaseValue);
        }

        public void ApplyNewValue(object target)
        {
            MemberMap.Setter(target, NewValue);
        }
    }

    /// <summary>
    /// 比较结果
    /// </summary>
    public class BsonDiffResult
    {
        public bool AreEqual { get; set; }
        public Dictionary<string, IBsonMemberDifference> Differences { get; set; } = new Dictionary<string, IBsonMemberDifference>();

        /// <summary>
        /// 获取指定字段名的差异
        /// </summary>
        public IBsonMemberDifference GetFieldDifference(string fieldName)
        {
            return Differences.TryGetValue(fieldName, out var difference) ? difference : null;
        }

        /// <summary>
        /// 检查指定字段是否有差异
        /// </summary>
        public bool HasFieldDifference(string fieldName)
        {
            return Differences.ContainsKey(fieldName);
        }

        /// <summary>
        /// 获取所有差异的集合
        /// </summary>
        public ICollection<IBsonMemberDifference> GetAllDifferences()
        {
            return Differences.Values;
        }

        /// <summary>
        /// 添加差异
        /// </summary>
        internal void AddDifference(IBsonMemberDifference difference)
        {
            Differences[difference.FieldName] = difference;
        }

        public static implicit operator bool(BsonDiffResult result) => result.AreEqual;
        public static BsonDiffResult Equal = new BsonDiffResult() { AreEqual = true };
    }

    /// <summary>Implements methods to support the comparison of objects for equality, in a customizable fashion.</summary>
    /// <typeparam name="T">The comparison object type.</typeparam>
    public class FuncEqualityComparer<T> : IEqualityComparer<T>
    {
        /// <summary>Implements methods to support the comparison of objects for equality, in a customizable fashion.</summary>
        /// <typeparam name="T">The comparison object type.</typeparam>
        private FuncEqualityComparer(Func<T, T, bool> func)
        {
            Func = func;
        }

        static readonly ConcurrentDictionary<Func<T, T, bool>, FuncEqualityComparer<T>> _cache = new();
        public static FuncEqualityComparer<T> Build(Func<T, T, bool> func)
        {
            return _cache.GetOrAdd(func, new FuncEqualityComparer<T>(func));
        }

        public Func<T, T, bool> Func { get; set; }

        /// <inheritdoc />
        public bool Equals(T? x, T? y)
        {
            if (ReferenceEquals(x, y)) return true;
            return Func(x, y);
        }

        /// <inheritdoc />
        public int GetHashCode([DisallowNull] T obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// 基于 BsonClassMap 配置的高性能对象比较器
    /// </summary>
    public static class BsonDataDiffer
    {
        private static readonly ConcurrentDictionary<Type, bool> _isSimpleTypeCache = new();
        private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> _equalityComparerCache = new();
        private static readonly ConcurrentDictionary<Type, MethodInfo> _diffMethodCache = new();

        // 预编译常用类型的比较委托，减少反射调用
        private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> _fastComparerCache = new();

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _isSimpleTypeCache.Clear();
            _equalityComparerCache.Clear();
            _diffMethodCache.Clear();
            _fastComparerCache.Clear();
        }

        private static bool? BasicCompare<T>(T baseObj, T newObj)
        {
            // Handle nulls
            if (baseObj == null && newObj == null) return true;
            if (baseObj == null || newObj == null) return false;

            if (ReferenceEquals(baseObj, newObj))
            {
                return true;
            }

            // Use appropriate comparison based on type
            if (baseObj is IEquatable<T> equatable)
                return equatable.Equals(newObj);

            return null;
        }

        /// <summary>
        /// 比较两个对象
        /// </summary>
        public static BsonDiffResult DiffEntity(CacheInfo typeCache, IEntityBase baseObj, IEntityBase newObj,
            BsonDiffMode mode = BsonDiffMode.Fast, IEnumerable<BsonMemberMap>? memberMapsToCompare = null)
        {
            var baseType = baseObj?.GetType() ?? typeCache.EntityType;
            var newType = newObj?.GetType() ?? typeCache.EntityType;
            var context = new DiffContext(mode, new BsonDiffResult() { AreEqual = false });
            var typeEqual = baseType == newType;
            if (!typeEqual && mode == BsonDiffMode.Fast)
            {
                return context.Result;
            }

            memberMapsToCompare ??= DB.GetCacheInfo(baseType).MemberMapsWithoutModifiedOn.AsEnumerable();

            if (baseObj == null)
            {
                context.Result.AreEqual = false;
                context.Result.Differences = memberMapsToCompare.Select(x => new BsonMemberDifference
                {
                    FieldName = x.ElementName,
                    FieldType = x.MemberType,
                    BaseValue = null,
                    NewValue = x.Getter(newObj),
                    MemberMap = x
                }).ToDictionary(x => x.FieldName, x => (IBsonMemberDifference)x);
                return context.Result;
            }

            foreach (var memberMap in memberMapsToCompare)
            {
                if (!context.ShouldContinue) break;

                try
                {

                    var baseValue = memberMap.Getter(baseObj);
                    var newValue = memberMap.Getter(newObj);

                    // 使用高效的相等性比较
                    if (!AreValuesEqual(baseValue, newValue, baseType, newType))
                    {
                        // 创建泛型差异对象，避免装拆箱（使用优化的工厂）
                        var difference = memberMap.CreateFieldDifference(memberMap.ElementName, baseValue, newValue);
                        context.Result.AddDifference(difference);
                    }
                }
                catch (Exception ex)
                {
                    // 记录获取成员值时的异常
                    var errorDifference = new BsonMemberDifference<string>
                    {
                        FieldName = memberMap.ElementName,
                        BaseValue = $"Error: {ex.Message}",
                        NewValue = $"Error: {ex.Message}",
                        MemberMap = memberMap
                    };
                    context.Result.AddDifference(errorDifference);
                }
            }
            context.Result.AreEqual = typeEqual && context.Result.Differences.Count == 0;
            return context.Result;
        }

        private class DiffContext
        {
            public BsonDiffMode Mode { get; set; }
            public virtual BsonDiffResult Result { get; }
            public bool ShouldContinue => Mode == BsonDiffMode.Full || Result.Differences.Count == 0;

            public DiffContext(BsonDiffMode mode, BsonDiffResult result)
            {
                Mode = mode;
                Result = result;
            }
        }

        private static readonly Dictionary<Type, Func<object, object, bool>> TypeComparers = new Dictionary<Type, Func<object, object, bool>>
        {
            [typeof(byte[])] = (a, b) => CompareByteArrays((byte[])a, (byte[])b),
            [typeof(JsonNode)] = (a, b) => CompareJsonNodes((JsonNode)a, (JsonNode)b),
            [typeof(JsonObject)] = (a, b) => CompareJsonNodes((JsonNode)a, (JsonNode)b),
            [typeof(JsonArray)] = (a, b) => CompareJsonNodes((JsonNode)a, (JsonNode)b),
            [typeof(JsonValue)] = (a, b) => CompareJsonNodes((JsonNode)a, (JsonNode)b),
            [typeof(DateTimeOffset)] = (a, b) => ((DateTimeOffset)a == (DateTimeOffset)b),
        };

        public static bool AreValuesEqual(object baseObj, object newObj, Type? baseType = null, Type? newType = null)
        {
            // 快速基础检查
            var basicCheckResult = BasicCompare(baseObj, newObj);
            if (basicCheckResult.HasValue)
                return basicCheckResult.Value;

            // 对于复杂类型的特殊处理
            baseType = baseObj.GetType();
            newType = newObj.GetType();
            if (Type.GetTypeCode(baseType) == TypeCode.Object)
                return HandleComplexTypes(baseObj, newObj, baseType, newType);

            // 所有基础类型直接使用 Equals
            return baseObj.Equals(newObj);
        }

        private static bool HandleComplexTypes(object baseObj, object newObj, Type baseType, Type newType)
        {
            // O(1) 字典查找特殊类型
            if (TypeComparers.TryGetValue(baseType, out var comparer))
            {
                return comparer(baseObj, newObj);
            }

            // 缓存接口检查结果以避免重复反射
            if (IsIEnumerationType(baseType))
            {
                return baseObj.ToString() == newObj.ToString();
            }

            if (IsIEnumerableType(baseType))
            {
                return CompareEnumerables((IEnumerable)baseObj, (IEnumerable)newObj);
            }

            if (IsIComparableType(baseType))
            {
                return (baseObj as IComparable).CompareTo(newObj as IComparable) == 0;
            }

            return baseObj.Equals(newObj);
        }

        // 缓存接口检查结果避免重复反射
        private static readonly ConcurrentDictionary<Type, bool> _isIEnumerationCache = new();
        private static readonly ConcurrentDictionary<Type, bool> _isIEnumerableCache = new();
        private static readonly ConcurrentDictionary<Type, bool> _isIComparableCache = new();

        private static bool IsIEnumerationType(Type type)
        {
            return _isIEnumerationCache.GetOrAdd(type, t => typeof(IEnumeration).IsAssignableFrom(t));
        }

        private static bool IsIEnumerableType(Type type)
        {
            return _isIEnumerableCache.GetOrAdd(type, t =>
                typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string));
        }

        private static bool IsIComparableType(Type type)
        {
            return _isIComparableCache.GetOrAdd(type, t => typeof(IComparable).IsAssignableFrom(t));
        }

        /// <summary>
        /// 高效的集合比较
        /// </summary>
        private static bool CompareEnumerables(IEnumerable baseEnumerable, IEnumerable newEnumerable)
        {
            if (ReferenceEquals(baseEnumerable, newEnumerable)) return true;
            if (baseEnumerable == null || newEnumerable == null) return false;

            var baseEnumerator = baseEnumerable.GetEnumerator();
            var newEnumerator = newEnumerable.GetEnumerator();
            while (baseEnumerator.MoveNext() && newEnumerator.MoveNext())
            {
                var current1 = baseEnumerator.Current;
                var current2 = newEnumerator.Current;
                if (!AreValuesEqual(current1, current2))
                    return false;
            }
            return !baseEnumerator.MoveNext() && !newEnumerator.MoveNext();
        }

        private static bool CompareByteArrays(byte[] baseArray, byte[] newArray)
        {
            if (baseArray.Length != newArray.Length) return false;
            if (baseArray.Length == 0) return true;

            // 快速比较首尾元素
            if (baseArray[0] != newArray[0] || baseArray[^1] != newArray[^1]) return false;

            // 完整比较
            for (int i = 0; i < baseArray.Length; i++)
            {
                if (baseArray[i] != newArray[i]) return false;
            }
            return true;
        }

        private static readonly FuncEqualityComparer<JsonNode> JsonArrayEqualityComparer = FuncEqualityComparer<JsonNode>.Build(CompareJsonNodes);
        private static readonly FuncEqualityComparer<KeyValuePair<string, JsonNode>> JsonObjectEqualityComparer = FuncEqualityComparer<KeyValuePair<string, JsonNode>>.Build((basePair, newPair) => basePair.Key == newPair.Key && CompareJsonNodes(basePair.Value, newPair.Value));
        private static bool CompareJsonNodes(JsonNode baseJson, JsonNode newJson)
        {
            if (ReferenceEquals(baseJson, newJson)) return true;
            if (baseJson == null || newJson == null) return false;
            try
            {
                switch (baseJson)
                {
                    case JsonValue baseValue when newJson is JsonValue newValue:
                        return baseValue.ToJsonString() == newValue.ToJsonString();
                    case JsonArray baseArray when newJson is JsonArray newArray:
                        {
                            if (baseArray.Count != newArray.Count) return false;
                            var baseArrayO = baseArray.Order();
                            var newArrayO = newArray.Order();
                            return baseArrayO.SequenceEqual(newArrayO, JsonArrayEqualityComparer);
                        }
                    case JsonObject baseObj when newJson is JsonObject newObj:
                        if (baseObj.Count != newObj.Count) return false;
                        var baseObjO = baseObj.Order();
                        var newObjO = newObj.Order();
                        return baseObjO.SequenceEqual(newObjO, JsonObjectEqualityComparer);
                    default:
                        return baseJson.ToString() == newJson.ToString();
                }
            }
            catch
            {
                // 如果序列化失败，回退到对象比较
                return baseJson.ToString() == newJson.ToString();
            }
        }
    }
}
