using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;

using MediatR;

using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace Mediator
{
    public static class MediatorExtensions
    {
        /// <summary>
        /// 映射缓存
        /// </summary>
        static ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> mapDictionary = new();
        /// <summary>
        /// 根据属性名称反射全量替换属性值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="value"></param>
        /// <param name="target"></param>
        /// <param name="ignoredPropNames">需要忽略的属性列表</param>
        [Obsolete("不推荐使用此方法, 仅针对值对象适用")]
        public static void SetEntity<T, TEntity>(this T value, TEntity target, params string[] ignoredPropNames) where T : IBaseRequest where TEntity : IEntityBase
        {
            var cachedSrcMap = mapDictionary.GetOrAdd(typeof(T), x => x.GetProperties(BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public).Where(y => y.CanRead).ToDictionary(y => y.Name, y => y));
            var cachedTargetMap = mapDictionary.GetOrAdd(typeof(TEntity), x => x.GetProperties(BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public).Where(y => y.CanWrite).ToDictionary(y => y.Name, y => y));
            var overlaps = cachedSrcMap.WhereIf(ignoredPropNames.Any(), x => !ignoredPropNames.Contains(x.Key)).Join(cachedTargetMap, l => l.Key, r => r.Key, (srcProp, targetProp) => (srcProp, targetProp));
            foreach (var ((_, srcProp), (_, targetProp)) in overlaps)
            {
                targetProp.SetValue(target, srcProp.GetValue(value));
            }
        }
    }
}
