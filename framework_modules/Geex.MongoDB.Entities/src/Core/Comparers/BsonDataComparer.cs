using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;

using Geex;

using MongoDB.Bson.Serialization;

namespace MongoDB.Entities.Core.Comparers
{
    /// <summary>
    /// 比较结果类型
    /// </summary>
    public enum BsonComparisonMode
    {
        /// <summary>
        /// 快速模式：找到第一个差异就停止
        /// </summary>
        FastMode,
        /// <summary>
        /// 完整模式：收集所有差异
        /// </summary>
        FullMode
    }

    /// <summary>
    /// 字段差异信息
    /// </summary>
    public class BsonFieldDifference
    {
        public string FieldPath { get; set; }
        public object Value1 { get; set; }
        public object Value2 { get; set; }
        public Type FieldType { get; set; }
    }

    /// <summary>
    /// 比较结果
    /// </summary>
    public class BsonComparisonResult
    {
        public bool AreEqual { get; set; }
        public List<BsonFieldDifference> Differences { get; set; } = new List<BsonFieldDifference>();
        public TimeSpan ElapsedTime { get; set; }
    }

    /// <summary>
    /// 基于 BsonClassMap 配置的高性能对象比较器
    /// </summary>
    public class BsonDataComparer
    {
        private static readonly ConcurrentDictionary<Type, BsonMemberMap[]> _memberMapCache = new();
        private static readonly ConcurrentDictionary<Type, bool> _isSimpleTypeCache = new();
        private static long _totalComparisons = 0;
        private static long _totalComparisonTime = 0;

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public static (long TotalComparisons, TimeSpan AverageTime, long CachedTypes) GetPerformanceStats()
        {
            var totalTime = TimeSpan.FromTicks(_totalComparisonTime);
            var avgTime = _totalComparisons > 0 ? TimeSpan.FromTicks(_totalComparisonTime / _totalComparisons) : TimeSpan.Zero;
            return (_totalComparisons, avgTime, _memberMapCache.Count + _isSimpleTypeCache.Count);
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _memberMapCache.Clear();
            _isSimpleTypeCache.Clear();
        }

        /// <summary>
        /// 创建差异字典，用于数据字段合并
        /// </summary>
        /// <param name="result">比较结果</param>
        /// <returns>差异字典，键为字段路径，值为差异信息</returns>
        public static Dictionary<string, BsonFieldDifference> CreateDifferenceDictionary(BsonComparisonResult result)
        {
            return result.Differences.ToDictionary(d => d.FieldPath, d => d);
        }

        /// <summary>
        /// 快速比较两个对象是否相等（仅返回 bool 结果，最高性能）
        /// </summary>
        public static bool AreEqual<T>(T obj1, T obj2)
        {
            if (ReferenceEquals(obj1, obj2)) return true;
            if (obj1 == null || obj2 == null) return false;

            var result = Compare(obj1, obj2, BsonComparisonMode.FastMode);
            return result.AreEqual;
        }

        /// <summary>
        /// 比较两个对象
        /// </summary>
        public static BsonComparisonResult Compare<T>(T obj1, T obj2, BsonComparisonMode mode = BsonComparisonMode.FastMode, Type? typeHint = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new BsonComparisonResult();
            var type = typeHint ?? typeof(T);
            try
            {
                // 更新统计计数
                Interlocked.Increment(ref _totalComparisons);

                if (ReferenceEquals(obj1, obj2))
                {
                    result.AreEqual = true;
                    return result;
                }

                if (obj1 == null || obj2 == null)
                {
                    result.AreEqual = false;
                    if (mode == BsonComparisonMode.FullMode)
                    {
                        result.Differences.Add(new BsonFieldDifference
                        {
                            FieldPath = "Root",
                            Value1 = obj1,
                            Value2 = obj2,
                            FieldType = type
                        });
                    }
                    return result;
                }

                var context = new ComparisonContext(mode, result);
                CompareObjects(obj1, obj2, type, "", context);
                result.AreEqual = result.Differences.Count == 0;
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedTime = stopwatch.Elapsed;

                // 更新总耗时统计
                Interlocked.Add(ref _totalComparisonTime, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }

        private class ComparisonContext
        {
            public BsonComparisonMode Mode { get; }
            public BsonComparisonResult Result { get; }
            public HashSet<(object, object)> VisitedPairs { get; } = new();

            public ComparisonContext(BsonComparisonMode mode, BsonComparisonResult result)
            {
                Mode = mode;
                Result = result;
            }

            public bool ShouldContinue => Mode == BsonComparisonMode.FullMode || Result.Differences.Count == 0;
        }

        private static void CompareObjects(object obj1, object obj2, Type type, string path, ComparisonContext context)
        {
            if (!context.ShouldContinue) return;

            // 避免循环引用
            var pair = (obj1, obj2);
            if (context.VisitedPairs.Contains(pair))
                return;
            context.VisitedPairs.Add(pair);

            try
            {
                if (IsSimpleType(type))
                {
                    CompareSimpleValues(obj1, obj2, type, path, context);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
                {
                    CompareEnumerables(obj1 as IEnumerable, obj2 as IEnumerable, type, path, context);
                }
                else
                {
                    CompareComplexObjects(obj1, obj2, type, path, context);
                }
            }
            finally
            {
                context.VisitedPairs.Remove(pair);
            }
        }

        private static void CompareSimpleValues(object obj1, object obj2, Type type, string path, ComparisonContext context)
        {
            bool areEqual = false;

            if (obj1 == null && obj2 == null)
            {
                areEqual = true;
            }
            else if (obj1 == null || obj2 == null)
            {
                areEqual = false;
            }
            else
            {
                // 特殊类型处理
                if (type == typeof(byte[]))
                {
                    areEqual = CompareByteArrays((byte[])obj1, (byte[])obj2);
                }
                else if (obj1 is JsonNode json1 && obj2 is JsonNode json2)
                {
                    areEqual = CompareJsonNodes(json1, json2);
                }
                else if (obj1 is IEnumeration enum1 && obj2 is IEnumeration enum2)
                {
                    areEqual = CompareEnumerations(enum1, enum2);
                }
                else if (obj1 is IEnumeration enum1Only)
                {
                    areEqual = CompareEnumerationWithObject(enum1Only, obj2);
                }
                else if (obj2 is IEnumeration enum2Only)
                {
                    areEqual = CompareEnumerationWithObject(enum2Only, obj1);
                }
                else if (obj1 is IComparable comparable1 && obj2 is IComparable comparable2)
                {
                    try
                    {
                        areEqual = comparable1.CompareTo(comparable2) == 0;
                    }
                    catch
                    {
                        areEqual = obj1.Equals(obj2);
                    }
                }
                else
                {
                    areEqual = obj1.Equals(obj2);
                }
            }

            if (!areEqual)
            {
                context.Result.Differences.Add(new BsonFieldDifference
                {
                    FieldPath = path,
                    Value1 = obj1,
                    Value2 = obj2,
                    FieldType = type
                });
            }
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

        private static bool CompareJsonNodes(JsonNode json1, JsonNode json2)
        {
            if (ReferenceEquals(json1, json2)) return true;
            if (json1 == null || json2 == null) return false;

            try
            {
                var str1 = json1.ToJsonString();
                var str2 = json2.ToJsonString();
                return str1 == str2;
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

        private static void CompareEnumerables(IEnumerable enum1, IEnumerable enum2, Type type, string path, ComparisonContext context)
        {
            if (!context.ShouldContinue) return;

            if (enum1 == null && enum2 == null) return;

            if (enum1 == null || enum2 == null)
            {
                context.Result.Differences.Add(new BsonFieldDifference
                {
                    FieldPath = path,
                    Value1 = enum1,
                    Value2 = enum2,
                    FieldType = type
                });
                return;
            }

            var list1 = enum1.Cast<object>().ToList();
            var list2 = enum2.Cast<object>().ToList();

            if (list1.Count != list2.Count)
            {
                context.Result.Differences.Add(new BsonFieldDifference
                {
                    FieldPath = $"{path}.Count",
                    Value1 = list1.Count,
                    Value2 = list2.Count,
                    FieldType = typeof(int)
                });

                if (context.Mode == BsonComparisonMode.FastMode) return;
            }

            var elementType = GetElementType(type);
            int maxCount = Math.Max(list1.Count, list2.Count);

            for (int i = 0; i < maxCount && context.ShouldContinue; i++)
            {
                var item1 = i < list1.Count ? list1[i] : null;
                var item2 = i < list2.Count ? list2[i] : null;

                string itemPath = $"{path}[{i}]";
                CompareObjects(item1, item2, elementType, itemPath, context);
            }
        }

        private static void CompareComplexObjects(object obj1, object obj2, Type type, string path, ComparisonContext context)
        {
            if (!context.ShouldContinue) return;

            var memberMaps = GetBsonMemberMaps(type);

            foreach (var memberMap in memberMaps)
            {
                if (!context.ShouldContinue) break;

                try
                {
                    var value1 = GetMemberValue(obj1, memberMap);
                    var value2 = GetMemberValue(obj2, memberMap);

                    string memberPath = string.IsNullOrEmpty(path) ? memberMap.ElementName : $"{path}.{memberMap.ElementName}";

                    CompareObjects(value1, value2, memberMap.MemberType, memberPath, context);
                }
                catch (Exception ex)
                {
                    // 记录获取成员值时的异常
                    context.Result.Differences.Add(new BsonFieldDifference
                    {
                        FieldPath = $"{path}.{memberMap.ElementName}",
                        Value1 = $"Error: {ex.Message}",
                        Value2 = $"Error: {ex.Message}",
                        FieldType = memberMap.MemberType
                    });
                }
            }
        }

        private static BsonMemberMap[] GetBsonMemberMaps(Type type)
        {
            return _memberMapCache.GetOrAdd(type, t =>
            {
                try
                {
                    var classMap = BsonClassMap.LookupClassMap(t);
                    return classMap.AllMemberMaps
                        .ToArray();
                }
                catch
                {
                    var classMap = new BsonClassMap(t);
                    BsonClassMap.RegisterClassMap(classMap);
                    classMap.AutoMap();
                    return classMap.AllMemberMaps
                         .ToArray();
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
            return _isSimpleTypeCache.GetOrAdd(type, t =>
            {
                return t.IsPrimitive ||
                       t == typeof(string) ||
                       t == typeof(decimal) ||
                       t == typeof(DateTime) ||
                       t == typeof(DateTimeOffset) ||
                       t == typeof(TimeSpan) ||
                       t == typeof(Guid) ||
                       t == typeof(byte[]) ||
                       t.IsAssignableTo<JsonNode>() ||
                       t.IsAssignableTo<IEnumeration>() ||
                       (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(t.GetGenericArguments()[0])) ||
                       t.IsEnum;
            });
        }

        private static Type GetElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
            {
                var genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length > 0)
                    return genericArgs[0];
            }

            return typeof(object);
        }
    }
}
