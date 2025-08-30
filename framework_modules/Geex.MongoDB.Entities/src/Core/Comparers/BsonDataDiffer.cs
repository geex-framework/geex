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
    public interface IBsonFieldDifference
    {
        string FieldName { get; }
        Type FieldType { get; }
        BsonMemberMap MemberMap { get; }
        bool HasBaseValue { get; }
        bool HasNewValue { get; }

        /// <summary>
        /// 将字段值应用到目标对象
        /// </summary>
        void ApplyBaseValueToTarget(object target);
        void ApplyNewValueToTarget(object target);

        /// <summary>
        /// 获取字段值（避免装拆箱的访问器）
        /// </summary>
        T GetBaseValue<T>();
        T GetNewValue<T>();
    }

    /// <summary>
    /// 泛型字段差异信息，避免装拆箱操作
    /// </summary>
    public class BsonFieldDifference<TField> : IBsonFieldDifference
    {
        public string FieldName { get; set; }
        public TField BaseValue { get; set; }
        public TField NewValue { get; set; }
        public BsonMemberMap MemberMap { get; set; }
        public bool HasBaseValue { get; set; } = true;
        public bool HasNewValue { get; set; } = true;

        public Type FieldType => typeof(TField);

        public void ApplyBaseValueToTarget(object target)
        {
            if (HasBaseValue && MemberMap != null)
                SetMemberValue(target, MemberMap, BaseValue);
        }

        public void ApplyNewValueToTarget(object target)
        {
            if (HasNewValue && MemberMap != null)
                SetMemberValue(target, MemberMap, NewValue);
        }

        public T GetBaseValue<T>()
        {
            if (typeof(T) == typeof(TField))
                return (T)(object)BaseValue;

            if (BaseValue is T value)
                return value;

            throw new InvalidCastException($"Cannot cast {typeof(TField)} to {typeof(T)}");
        }

        public T GetNewValue<T>()
        {
            if (typeof(T) == typeof(TField))
                return (T)(object)NewValue;

            if (NewValue is T value)
                return value;

            throw new InvalidCastException($"Cannot cast {typeof(TField)} to {typeof(T)}");
        }

        private static void SetMemberValue(object obj, BsonMemberMap memberMap, object value)
        {
            if (memberMap.MemberInfo is PropertyInfo property)
                property.SetValue(obj, value);
            else if (memberMap.MemberInfo is FieldInfo field)
                field.SetValue(obj, value);
        }
    }

    /// <summary>
    /// 泛型比较结果，提供类型安全的字段访问
    /// </summary>
    public class BsonDiffResult<TEntity> : BsonDiffResult
    {
        /// <summary>
        /// 获取指定类型的字段差异
        /// </summary>
        public IEnumerable<BsonFieldDifference<TField>> GetFieldDifferences<TField>()
        {
            return Differences.Values.OfType<BsonFieldDifference<TField>>();
        }

        /// <summary>
        /// 获取指定字段名和类型的差异
        /// </summary>
        public BsonFieldDifference<TField> GetFieldDifference<TField>(string fieldName)
        {
            return GetFieldDifference(fieldName) as BsonFieldDifference<TField>;
        }

        /// <summary>
        /// 根据表达式获取字段名的差异
        /// </summary>
        public IBsonFieldDifference GetFieldDifference<TField>(Expression<Func<TEntity, TField>> expression)
        {
            var fieldName = GetFieldNameFromExpression(expression);
            return GetFieldDifference(fieldName);
        }

        /// <summary>
        /// 根据表达式获取强类型字段差异
        /// </summary>
        public BsonFieldDifference<TField> GetTypedFieldDifference<TField>(Expression<Func<TEntity, TField>> expression)
        {
            var fieldName = GetFieldNameFromExpression(expression);
            return GetFieldDifference<TField>(fieldName);
        }

        /// <summary>
        /// 将所有差异中的BaseValue应用到目标对象
        /// </summary>
        public void ApplyBaseValueToTarget(TEntity target)
        {
            foreach (var diff in Differences.Values)
                diff.ApplyBaseValueToTarget(target);
        }

        /// <summary>
        /// 将所有差异中的NewValue应用到目标对象
        /// </summary>
        public void ApplyNewValueToTarget(TEntity target)
        {
            foreach (var diff in Differences.Values)
                diff.ApplyNewValueToTarget(target);
        }

        /// <summary>
        /// 应用指定字段的BaseValue到目标对象
        /// </summary>
        public void ApplyFieldBaseValueToTarget(TEntity target, string fieldName)
        {
            if (Differences.TryGetValue(fieldName, out var difference))
                difference.ApplyBaseValueToTarget(target);
        }

        /// <summary>
        /// 应用指定字段的NewValue到目标对象
        /// </summary>
        public void ApplyFieldNewValueToTarget(TEntity target, string fieldName)
        {
            if (Differences.TryGetValue(fieldName, out var difference))
                difference.ApplyNewValueToTarget(target);
        }

        /// <summary>
        /// 根据表达式应用指定字段的BaseValue到目标对象
        /// </summary>
        public void ApplyFieldBaseValueToTarget<TField>(TEntity target, Expression<Func<TEntity, TField>> expression)
        {
            var fieldName = GetFieldNameFromExpression(expression);
            ApplyFieldBaseValueToTarget(target, fieldName);
        }

        /// <summary>
        /// 根据表达式应用指定字段的NewValue到目标对象
        /// </summary>
        public void ApplyFieldNewValueToTarget<TField>(TEntity target, Expression<Func<TEntity, TField>> expression)
        {
            var fieldName = GetFieldNameFromExpression(expression);
            ApplyFieldNewValueToTarget(target, fieldName);
        }

        /// <summary>
        /// 批量应用指定字段的BaseValue到目标对象
        /// </summary>
        public void ApplyFieldsBaseValueToTarget(TEntity target, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                ApplyFieldBaseValueToTarget(target, fieldName);
            }
        }

        /// <summary>
        /// 批量应用指定字段的NewValue到目标对象
        /// </summary>
        public void ApplyFieldsNewValueToTarget(TEntity target, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                ApplyFieldNewValueToTarget(target, fieldName);
            }
        }

        /// <summary>
        /// 从表达式中提取字段名
        /// </summary>
        private static string GetFieldNameFromExpression<TField>(Expression<Func<TEntity, TField>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a member access expression", nameof(expression));
        }

        public static implicit operator bool(BsonDiffResult<TEntity> result) => result.AreEqual;
    }

    /// <summary>
    /// 向后兼容的非泛型比较结果
    /// </summary>
    public class BsonDiffResult
    {
        public bool AreEqual { get; set; }
        public Dictionary<string, IBsonFieldDifference> Differences { get; set; } = new Dictionary<string, IBsonFieldDifference>();

        /// <summary>
        /// 获取指定字段名的差异
        /// </summary>
        public IBsonFieldDifference GetFieldDifference(string fieldName)
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
        /// 获取所有差异的集合（向后兼容）
        /// </summary>
        public ICollection<IBsonFieldDifference> GetAllDifferences()
        {
            return Differences.Values;
        }

        /// <summary>
        /// 添加差异
        /// </summary>
        internal void AddDifference(IBsonFieldDifference difference)
        {
            Differences[difference.FieldName] = difference;
        }

        public static implicit operator bool(BsonDiffResult result) => result.AreEqual;

        /// <summary>
        /// 从泛型结果创建非泛型结果
        /// </summary>
        public static BsonDiffResult FromGeneric<T>(BsonDiffResult<T> genericResult)
        {
            return new BsonDiffResult
            {
                AreEqual = genericResult.AreEqual,
                Differences = genericResult.Differences
            };
        }
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
        
        // 常用类型的预编译比较器
        private static readonly Func<object, object, bool> StringComparer = (a, b) => string.Equals((string)a, (string)b);
        private static readonly Func<object, object, bool> IntComparer = (a, b) => (int)a == (int)b;
        private static readonly Func<object, object, bool> LongComparer = (a, b) => (long)a == (long)b;
        private static readonly Func<object, object, bool> BoolComparer = (a, b) => (bool)a == (bool)b;
        private static readonly Func<object, object, bool> DateTimeComparer = (a, b) => (DateTime)a == (DateTime)b;
        private static readonly Func<object, object, bool> DateTimeOffsetComparer = (a, b) => (DateTimeOffset)a == (DateTimeOffset)b;

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

        /// <summary>
        /// 获取差异字典（现在直接返回内部字典的副本）
        /// </summary>
        /// <param name="result">比较结果</param>
        /// <returns>差异字典，键为字段名，值为差异信息</returns>
        public static Dictionary<string, IBsonFieldDifference> CreateDifferenceDictionary<TEntity>(BsonDiffResult<TEntity> result)
        {
            return new Dictionary<string, IBsonFieldDifference>(result.Differences);
        }

        /// <summary>
        /// 向后兼容的差异字典创建方法
        /// </summary>
        public static Dictionary<string, IBsonFieldDifference> CreateDifferenceDictionary(BsonDiffResult result)
        {
            return new Dictionary<string, IBsonFieldDifference>(result.Differences);
        }

        /// <summary>
        /// 获取差异字典的只读视图（高性能，无复制）
        /// </summary>
        /// <param name="result">比较结果</param>
        /// <returns>差异字典的只读视图</returns>
        public static IReadOnlyDictionary<string, IBsonFieldDifference> GetDifferenceDictionary<TEntity>(BsonDiffResult<TEntity> result)
        {
            return result.Differences;
        }

        /// <summary>
        /// 向后兼容的差异字典只读视图获取方法
        /// </summary>
        public static IReadOnlyDictionary<string, IBsonFieldDifference> GetDifferenceDictionary(BsonDiffResult result)
        {
            return result.Differences;
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
        private static readonly MethodInfo DiffMethod = typeof(BsonDataDiffer).GetMethod(nameof(Diff), BindingFlags.Public | BindingFlags.Static);


        /// <summary>
        /// 比较两个对象
        /// </summary>
        public static BsonDiffResult<T> Diff<T>(T baseObj, T newObj, BsonDiffMode mode = BsonDiffMode.Fast)
        {
            // 当泛型类型是IEntityBase时，需要使用实际类型进行比较以获得正确的BsonClassMap
            if (typeof(T) == typeof(IEntityBase))
            {
                // 处理null情况
                if (baseObj == null && newObj == null)
                {
                    return new BsonDiffResult<T> { AreEqual = true };
                }
                if (baseObj == null || newObj == null)
                {
                    return new BsonDiffResult<T> { AreEqual = false };
                }

                var baseType = baseObj.GetType();
                var newType = newObj.GetType();

                // 如果两个对象的实际类型不同，直接返回不相等
                if (baseType != newType)
                {
                    return new BsonDiffResult<T> { AreEqual = false };
                }

                // 使用实际类型进行比较（使用缓存提高性能）
                var cachedMethod = _diffMethodCache.GetOrAdd(baseType, type => DiffMethod.MakeGenericMethod(type));
                var actualResult = cachedMethod.Invoke(null, [baseObj, newObj, mode]);

                // 创建正确的泛型结果类型
                var genericResult = new BsonDiffResult<T>
                {
                    AreEqual = ((BsonDiffResult)actualResult).AreEqual,
                    Differences = ((BsonDiffResult)actualResult).Differences
                };

                return genericResult;
            }

            var result = new BsonDiffResult<T>();

            var basicCheckResult = BasicCompare(baseObj, newObj);

            if (basicCheckResult.HasValue)
            {
                result.AreEqual = basicCheckResult.Value;
                return result;
            }

            var context = new DiffContext<T>(mode, result);
            DiffTopLevelFields(baseObj, newObj, context);
            result.AreEqual = result.Differences.Count == 0;

            return result;
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
        private class DiffContext<T> : DiffContext
        {
            public override BsonDiffResult<T> Result => (BsonDiffResult<T>)base.Result;

            /// <inheritdoc />
            public DiffContext(BsonDiffMode mode, BsonDiffResult result) : base(mode, result)
            {
            }
        }
        private static readonly MethodInfo DiffTopLevelFieldsMethod = typeof(BsonDataDiffer).GetMethod(nameof(DiffTopLevelFields), BindingFlags.NonPublic | BindingFlags.Static);
        /// <summary>
        /// 比较顶层字段，不深入嵌套对象
        /// </summary>
        private static void DiffTopLevelFields<T>(T baseObj, T newObj, DiffContext<T> context)
        {
            var type = typeof(T);
            BsonMemberMap[] memberMaps;
            
            // 如果T是IEntityBase，使用Cache<T>的方法
            if (typeof(IEntityBase).IsAssignableFrom(type))
            {
                // 使用反射获取Cache<T>的GetBsonMemberMaps方法
                var cacheType = typeof(Cache<>).MakeGenericType(type);
                var getBsonMemberMapsMethod = cacheType.GetMethod("GetBsonMemberMaps", BindingFlags.Public | BindingFlags.Static);
                memberMaps = (BsonMemberMap[])getBsonMemberMapsMethod.Invoke(null, null);
            }
            else
            {
                memberMaps = GetBsonMemberMapsForType(type);
            }

            foreach (var memberMap in memberMaps)
            {
                if (!context.ShouldContinue) break;

                try
                {
                    var baseValue = MemberReflectionCache.GetValue(baseObj, memberMap);
                    var newValue = MemberReflectionCache.GetValue(newObj, memberMap);

                    // 使用高效的相等性比较
                    if (!AreValuesEqual(baseValue, newValue, memberMap.MemberType))
                    {
                        // 创建泛型差异对象，避免装拆箱（使用优化的工厂）
                        var difference = FieldDifferenceFactory.CreateFieldDifference(memberMap.ElementName, baseValue, newValue, memberMap);
                        context.Result.AddDifference(difference);
                    }
                }
                catch (Exception ex)
                {
                    // 记录获取成员值时的异常
                    var errorDifference = new BsonFieldDifference<string>
                    {
                        FieldName = memberMap.ElementName,
                        BaseValue = $"Error: {ex.Message}",
                        NewValue = $"Error: {ex.Message}",
                        MemberMap = memberMap
                    };
                    context.Result.AddDifference(errorDifference);
                }
            }
        }



        /// <summary>
        /// 高效的值相等性比较，支持各种特殊类型
        /// </summary>
        private static bool AreValuesEqual(object baseObj, object newObj, Type type)
        {
            var basicCheckResult = BasicCompare(baseObj, newObj);
            if (basicCheckResult.HasValue)
            {
                return basicCheckResult.Value;
            }

            // 首先尝试使用快速比较器
            var fastComparer = GetFastComparer(type);
            if (fastComparer != null)
            {
                return fastComparer(baseObj, newObj);
            }

            // 获取或创建高效的比较器
            var comparer = _equalityComparerCache.GetOrAdd(type, CreateEqualityComparer);
            return comparer(baseObj, newObj);
        }

        /// <summary>
        /// 获取预编译的快速比较器
        /// </summary>
        private static Func<object, object, bool> GetFastComparer(Type type)
        {
            return _fastComparerCache.GetOrAdd(type, t =>
            {
                return t switch
                {
                    Type when t == typeof(string) => StringComparer,
                    Type when t == typeof(int) => IntComparer,
                    Type when t == typeof(long) => LongComparer,
                    Type when t == typeof(bool) => BoolComparer,
                    Type when t == typeof(DateTime) => DateTimeComparer,
                    Type when t == typeof(DateTimeOffset) => DateTimeOffsetComparer,
                    _ => null
                };
            });
        }

        /// <summary>
        /// 为指定类型创建优化的相等性比较器
        /// </summary>
        private static Func<object, object, bool> CreateEqualityComparer(Type type)
        {
            // 字节数组特殊处理
            if (type == typeof(byte[]))
            {
                return (baseObj, newObj) => CompareByteArrays((byte[])baseObj, (byte[])newObj);
            }

            // JsonNode特殊处理
            if (typeof(JsonNode).IsAssignableFrom(type))
            {
                return (baseObj, newObj) => CompareJsonNodes((JsonNode)baseObj, (JsonNode)newObj);
            }

            // IEnumeration特殊处理
            if (typeof(IEnumeration).IsAssignableFrom(type))
            {
                return (baseObj, newObj) =>
                {
                    if (baseObj is IEnumeration baseEnum && newObj is IEnumeration enum2)
                        return CompareEnumerations(baseEnum, enum2);
                    if (baseObj is IEnumeration baseEnumOnly)
                        return CompareEnumerationWithObject(baseEnumOnly, newObj);
                    if (newObj is IEnumeration newEnumOnly)
                        return CompareEnumerationWithObject(newEnumOnly, baseObj);
                    return false;
                };
            }

            // 集合类型处理
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                return (baseObj, newObj) => CompareEnumerables((IEnumerable)baseObj, (IEnumerable)newObj);
            }

            // IComparable处理
            if (typeof(IComparable).IsAssignableFrom(type))
            {
                return (baseObj, newObj) =>
                {
                    try
                    {
                        return ((IComparable)baseObj).CompareTo(newObj) == 0;
                    }
                    catch
                    {
                        return baseObj.Equals(newObj);
                    }
                };
            }

            // 其他复杂对象使用深层比较
            if (!IsSimpleType(type))
            {
                return (baseObj, newObj) => Diff(baseObj, newObj);
            }

            // 默认使用Equals
            return (baseObj, newObj) => baseObj.Equals(newObj);
        }

        /// <summary>
        /// 高效的集合比较
        /// </summary>
        private static bool CompareEnumerables(IEnumerable baseEnumerable, IEnumerable newEnumerable)
        {
            if (ReferenceEquals(baseEnumerable, newEnumerable)) return true;
            if (baseEnumerable == null || newEnumerable == null) return false;

            var baseList = baseEnumerable.Cast<object>().ToList();
            var newList = newEnumerable.Cast<object>().ToList();

            if (baseList.Count != newList.Count) return false;

            for (int i = 0; i < baseList.Count; i++)
            {
                if (!Equals(baseList[i], newList[i])) return false;
            }

            return true;
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

        private static bool CompareEnumerations(IEnumeration baseEnum, IEnumeration newEnum)
        {
            if (ReferenceEquals(baseEnum, newEnum)) return true;
            if (baseEnum == null || newEnum == null) return false;

            return baseEnum.Value == newEnum.Value;
        }

        private static bool CompareEnumerationWithObject(IEnumeration enumeration, object other)
        {
            if (enumeration == null && other == null) return true;
            if (enumeration == null || other == null) return false;

            // 如果另一个对象也是 IEnumeration，使用专门的方法
            if (other is IEnumeration otherEnum)
            {
                return CompareEnumerations(enumeration, otherEnum);
            }

            // 比较枚举值和对象的字符串表示
            return enumeration.Value == other.ToString();
        }



        /// <summary>
        /// Gets BsonMemberMaps for non-entity types
        /// </summary>
        private static BsonMemberMap[] GetBsonMemberMapsForType(Type type)
        {
            try
            {
                var classMap = BsonClassMap.LookupClassMap(type);
                return classMap.AllMemberMaps.Where(x => x.MemberName != nameof(IEntityBase.ModifiedOn)).ToArray();
            }
            catch
            {
                var classMap = new BsonClassMap(type);
                BsonClassMap.RegisterClassMap(classMap);
                classMap.AutoMap();
                return classMap.AllMemberMaps.Where(x => x.MemberName != nameof(IEntityBase.ModifiedOn)).ToArray();
            }
        }



        private static bool IsSimpleType(Type type)
        {
            return _isSimpleTypeCache.GetOrAdd(type, t => t.IsPrimitive ||
                                                          type == typeof(string) ||
                                                          type == typeof(decimal) ||
                                                          type == typeof(DateTime) ||
                                                          type == typeof(DateTimeOffset) ||
                                                          type == typeof(TimeSpan) ||
                                                          type == typeof(Guid) ||
                                                          type == typeof(byte[]) ||
                                                          type.IsAssignableTo<JsonNode>() ||
                                                          type.IsAssignableTo<IEnumeration>() ||
                                                          (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0])) ||
                                                          type.IsEnum);

        }


    }
}
