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
        FastMode,
        /// <summary>
        /// 完整模式：收集所有顶层字段差异
        /// </summary>
        FullMode
    }

    /// <summary>
    /// 字段差异接口，支持类型安全的字段访问
    /// </summary>
    public interface IBsonFieldDifference
    {
        string FieldName { get; }
        Type FieldType { get; }
        BsonMemberMap MemberMap { get; }
        bool HasValue1 { get; }
        bool HasValue2 { get; }

        /// <summary>
        /// 将字段值应用到目标对象
        /// </summary>
        void ApplyValue1ToTarget(object target);
        void ApplyValue2ToTarget(object target);

        /// <summary>
        /// 获取字段值（避免装拆箱的访问器）
        /// </summary>
        T GetValue1<T>();
        T GetValue2<T>();
    }

    /// <summary>
    /// 泛型字段差异信息，避免装拆箱操作
    /// </summary>
    public class BsonFieldDifference<TField> : IBsonFieldDifference
    {
        public string FieldName { get; set; }
        public TField Value1 { get; set; }
        public TField Value2 { get; set; }
        public BsonMemberMap MemberMap { get; set; }
        public bool HasValue1 { get; set; } = true;
        public bool HasValue2 { get; set; } = true;

        public Type FieldType => typeof(TField);

        public void ApplyValue1ToTarget(object target)
        {
            if (HasValue1 && MemberMap != null)
                SetMemberValue(target, MemberMap, Value1);
        }

        public void ApplyValue2ToTarget(object target)
        {
            if (HasValue2 && MemberMap != null)
                SetMemberValue(target, MemberMap, Value2);
        }

        public T GetValue1<T>()
        {
            if (typeof(T) == typeof(TField))
                return (T)(object)Value1;

            if (Value1 is T value)
                return value;

            throw new InvalidCastException($"Cannot cast {typeof(TField)} to {typeof(T)}");
        }

        public T GetValue2<T>()
        {
            if (typeof(T) == typeof(TField))
                return (T)(object)Value2;

            if (Value2 is T value)
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
        /// 将所有差异中的Value1应用到目标对象
        /// </summary>
        public void ApplyValue1ToTarget(TEntity target)
        {
            foreach (var diff in Differences.Values)
                diff.ApplyValue1ToTarget(target);
        }

        /// <summary>
        /// 将所有差异中的Value2应用到目标对象
        /// </summary>
        public void ApplyValue2ToTarget(TEntity target)
        {
            foreach (var diff in Differences.Values)
                diff.ApplyValue2ToTarget(target);
        }

        /// <summary>
        /// 应用指定字段的Value1到目标对象
        /// </summary>
        public void ApplyFieldValue1ToTarget(TEntity target, string fieldName)
        {
            if (Differences.TryGetValue(fieldName, out var difference))
                difference.ApplyValue1ToTarget(target);
        }

        /// <summary>
        /// 应用指定字段的Value2到目标对象
        /// </summary>
        public void ApplyFieldValue2ToTarget(TEntity target, string fieldName)
        {
            if (Differences.TryGetValue(fieldName, out var difference))
                difference.ApplyValue2ToTarget(target);
        }

        /// <summary>
        /// 根据表达式应用指定字段的Value1到目标对象
        /// </summary>
        public void ApplyFieldValue1ToTarget<TField>(TEntity target, Expression<Func<TEntity, TField>> expression)
        {
            var fieldName = GetFieldNameFromExpression(expression);
            ApplyFieldValue1ToTarget(target, fieldName);
        }

        /// <summary>
        /// 根据表达式应用指定字段的Value2到目标对象
        /// </summary>
        public void ApplyFieldValue2ToTarget<TField>(TEntity target, Expression<Func<TEntity, TField>> expression)
        {
            var fieldName = GetFieldNameFromExpression(expression);
            ApplyFieldValue2ToTarget(target, fieldName);
        }

        /// <summary>
        /// 批量应用指定字段的Value1到目标对象
        /// </summary>
        public void ApplyFieldsValue1ToTarget(TEntity target, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                ApplyFieldValue1ToTarget(target, fieldName);
            }
        }

        /// <summary>
        /// 批量应用指定字段的Value2到目标对象
        /// </summary>
        public void ApplyFieldsValue2ToTarget(TEntity target, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                ApplyFieldValue2ToTarget(target, fieldName);
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
        private static readonly ConcurrentDictionary<Type, BsonMemberMap[]> _memberMapCache = new();
        private static readonly ConcurrentDictionary<Type, bool> _isSimpleTypeCache = new();
        private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> _equalityComparerCache = new();
        private static readonly ConcurrentDictionary<Type, MethodInfo> _diffMethodCache = new();

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _memberMapCache.Clear();
            _isSimpleTypeCache.Clear();
            _equalityComparerCache.Clear();
            _diffMethodCache.Clear();
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

        private static bool? BasicCompare<T>(T obj1, T obj2)
        {
            // Handle nulls
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;

            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            // Use appropriate comparison based on type
            if (obj1 is IEquatable<T> equatable)
                return equatable.Equals(obj2);

            return null;
        }
        private static readonly MethodInfo DiffMethod = typeof(BsonDataDiffer).GetMethod(nameof(Diff), BindingFlags.Public | BindingFlags.Static);


        /// <summary>
        /// 比较两个对象
        /// </summary>
        public static BsonDiffResult<T> Diff<T>(T obj1, T obj2, BsonDiffMode mode = BsonDiffMode.FastMode)
        {
            // 当泛型类型是IEntityBase时，需要使用实际类型进行比较以获得正确的BsonClassMap
            if (typeof(T) == typeof(IEntityBase))
            {
                // 处理null情况
                if (obj1 == null && obj2 == null)
                {
                    return new BsonDiffResult<T> { AreEqual = true };
                }
                if (obj1 == null || obj2 == null)
                {
                    return new BsonDiffResult<T> { AreEqual = false };
                }

                var type1 = obj1.GetType();
                var type2 = obj2.GetType();
                
                // 如果两个对象的实际类型不同，直接返回不相等
                if (type1 != type2)
                {
                    return new BsonDiffResult<T> { AreEqual = false };
                }

                // 使用实际类型进行比较（使用缓存提高性能）
                var cachedMethod = _diffMethodCache.GetOrAdd(type1, type => DiffMethod.MakeGenericMethod(type));
                var actualResult = cachedMethod.Invoke(null, [obj1, obj2, mode]);
                
                // 创建正确的泛型结果类型
                var genericResult = new BsonDiffResult<T>
                {
                    AreEqual = ((BsonDiffResult)actualResult).AreEqual,
                    Differences = ((BsonDiffResult)actualResult).Differences
                };
                
                return genericResult;
            }

            var result = new BsonDiffResult<T>();

            var basicCheckResult = BasicCompare(obj1, obj2);

            if (basicCheckResult.HasValue)
            {
                result.AreEqual = basicCheckResult.Value;
                return result;
            }

            var context = new DiffContext<T>(mode, result);
            DiffTopLevelFields(obj1, obj2, context);
            result.AreEqual = result.Differences.Count == 0;

            return result;
        }

        private class DiffContext
        {
            public BsonDiffMode Mode { get; set; }
            public virtual BsonDiffResult Result { get; }
            public bool ShouldContinue => Mode == BsonDiffMode.FullMode || Result.Differences.Count == 0;

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
        private static void DiffTopLevelFields<T>(T obj1, T obj2, DiffContext<T> context)
        {
            var type = typeof(T);
            var memberMaps = GetBsonMemberMaps(type);

            foreach (var memberMap in memberMaps)
            {
                if (!context.ShouldContinue) break;

                try
                {
                    var value1 = GetMemberValue(obj1, memberMap);
                    var value2 = GetMemberValue(obj2, memberMap);

                    // 使用高效的相等性比较
                    if (!AreValuesEqual(value1, value2, memberMap.MemberType))
                    {
                        // 创建泛型差异对象，避免装拆箱
                        var difference = CreateFieldDifference(memberMap.ElementName, value1, value2, memberMap);
                        context.Result.AddDifference(difference);
                    }
                }
                catch (Exception ex)
                {
                    // 记录获取成员值时的异常
                    var errorDifference = new BsonFieldDifference<string>
                    {
                        FieldName = memberMap.ElementName,
                        Value1 = $"Error: {ex.Message}",
                        Value2 = $"Error: {ex.Message}",
                        MemberMap = memberMap
                    };
                    context.Result.AddDifference(errorDifference);
                }
            }
        }

        /// <summary>
        /// 创建类型安全的字段差异对象
        /// </summary>
        private static IBsonFieldDifference CreateFieldDifference(string fieldName, object value1, object value2, BsonMemberMap memberMap)
        {
            var fieldType = memberMap.MemberType;

            // 使用反射创建正确类型的BsonFieldDifference<T>
            var differenceType = typeof(BsonFieldDifference<>).MakeGenericType(fieldType);
            var difference = (IBsonFieldDifference)Activator.CreateInstance(differenceType);

            // 设置属性值
            var fieldNameProperty = differenceType.GetProperty(nameof(BsonFieldDifference<object>.FieldName));
            var value1Property = differenceType.GetProperty(nameof(BsonFieldDifference<object>.Value1));
            var value2Property = differenceType.GetProperty(nameof(BsonFieldDifference<object>.Value2));
            var memberMapProperty = differenceType.GetProperty(nameof(BsonFieldDifference<object>.MemberMap));

            fieldNameProperty?.SetValue(difference, fieldName);
            value1Property?.SetValue(difference, value1);
            value2Property?.SetValue(difference, value2);
            memberMapProperty?.SetValue(difference, memberMap);

            return difference;
        }

        /// <summary>
        /// 高效的值相等性比较，支持各种特殊类型
        /// </summary>
        private static bool AreValuesEqual(object obj1, object obj2, Type type)
        {
            var basicCheckResult = BasicCompare(obj1, obj2);
            if (basicCheckResult.HasValue)
            {
                return basicCheckResult.Value;
            }

            // 获取或创建高效的比较器
            var comparer = _equalityComparerCache.GetOrAdd(type, CreateEqualityComparer);
            return comparer(obj1, obj2);
        }

        /// <summary>
        /// 为指定类型创建优化的相等性比较器
        /// </summary>
        private static Func<object, object, bool> CreateEqualityComparer(Type type)
        {
            // 字节数组特殊处理
            if (type == typeof(byte[]))
            {
                return (obj1, obj2) => CompareByteArrays((byte[])obj1, (byte[])obj2);
            }

            // JsonNode特殊处理
            if (typeof(JsonNode).IsAssignableFrom(type))
            {
                return (obj1, obj2) => CompareJsonNodes((JsonNode)obj1, (JsonNode)obj2);
            }

            // IEnumeration特殊处理
            if (typeof(IEnumeration).IsAssignableFrom(type))
            {
                return (obj1, obj2) =>
                {
                    if (obj1 is IEnumeration enum1 && obj2 is IEnumeration enum2)
                        return CompareEnumerations(enum1, enum2);
                    if (obj1 is IEnumeration enum1Only)
                        return CompareEnumerationWithObject(enum1Only, obj2);
                    if (obj2 is IEnumeration enum2Only)
                        return CompareEnumerationWithObject(enum2Only, obj1);
                    return false;
                };
            }

            // 集合类型处理
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                return (obj1, obj2) => CompareEnumerables((IEnumerable)obj1, (IEnumerable)obj2);
            }

            // IComparable处理
            if (typeof(IComparable).IsAssignableFrom(type))
            {
                return (obj1, obj2) =>
                {
                    try
                    {
                        return ((IComparable)obj1).CompareTo(obj2) == 0;
                    }
                    catch
                    {
                        return obj1.Equals(obj2);
                    }
                };
            }

            // 其他复杂对象使用深层比较
            if (!IsSimpleType(type))
            {
                return (obj1, obj2) => Diff(obj1, obj2);
            }

            // 默认使用Equals
            return (obj1, obj2) => obj1.Equals(obj2);
        }

        /// <summary>
        /// 高效的集合比较
        /// </summary>
        private static bool CompareEnumerables(IEnumerable enum1, IEnumerable enum2)
        {
            if (ReferenceEquals(enum1, enum2)) return true;
            if (enum1 == null || enum2 == null) return false;

            var list1 = enum1.Cast<object>().ToList();
            var list2 = enum2.Cast<object>().ToList();

            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!Equals(list1[i], list2[i])) return false;
            }

            return true;
        }

        private static bool CompareByteArrays(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length) return false;
            if (arr1.Length == 0) return true;

            // 快速比较首尾元素
            if (arr1[0] != arr2[0] || arr1[^1] != arr2[^1]) return false;

            // 完整比较
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i]) return false;
            }
            return true;
        }

        private static readonly FuncEqualityComparer<JsonNode> JsonArrayEqualityComparer = FuncEqualityComparer<JsonNode>.Build(CompareJsonNodes);
        private static readonly FuncEqualityComparer<KeyValuePair<string, JsonNode>> JsonObjectEqualityComparer = FuncEqualityComparer<KeyValuePair<string, JsonNode>>.Build((node1, node2) => node1.Key == node2.Key && CompareJsonNodes(node1.Value, node2.Value));
        private static bool CompareJsonNodes(JsonNode json1, JsonNode json2)
        {
            if (ReferenceEquals(json1, json2)) return true;
            if (json1 == null || json2 == null) return false;
            try
            {
                switch (json1)
                {
                    case JsonValue v1 when json2 is JsonValue v2:
                        return v1.ToJsonString() == v2.ToJsonString();
                    case JsonArray a1 when json2 is JsonArray a2:
                        {
                            if (a1.Count != a2.Count) return false;
                            var a1o = a1.Order();
                            var a2o = a2.Order();
                            return a1o.SequenceEqual(a2o, JsonArrayEqualityComparer);
                        }
                    case JsonObject o1 when json2 is JsonObject o2:
                        if (o1.Count != o2.Count) return false;
                        var o1o = o1.Order();
                        var o2o = o2.Order();
                        return o1o.SequenceEqual(o2o, JsonObjectEqualityComparer);
                    default:
                        return json1.ToString() == json2.ToString();
                }
            }
            catch
            {
                // 如果序列化失败，回退到对象比较
                return json1.ToString() == json2.ToString();
            }
        }

        private static bool CompareEnumerations(IEnumeration enum1, IEnumeration enum2)
        {
            if (ReferenceEquals(enum1, enum2)) return true;
            if (enum1 == null || enum2 == null) return false;

            return enum1.Value == enum2.Value;
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



        private static BsonMemberMap[] GetBsonMemberMaps(Type type)
        {
            return _memberMapCache.GetOrAdd(type, t =>
            {
                try
                {
                    var classMap = BsonClassMap.LookupClassMap(t);
                    return classMap.AllMemberMaps.ToArray();
                }
                catch
                {
                    var classMap = new BsonClassMap(t);
                    BsonClassMap.RegisterClassMap(classMap);
                    classMap.AutoMap();
                    return classMap.AllMemberMaps.ToArray();
                }
            });
        }

        private static object GetMemberValue(object obj, BsonMemberMap memberMap)
        {
            if (memberMap.MemberInfo is PropertyInfo property)
            {
                return property.GetValue(obj);
            }
            else if (memberMap.MemberInfo is FieldInfo field)
            {
                return field.GetValue(obj);
            }
            return null;
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
