using System;
using System.Reflection;

using MongoDB.Entities;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadConfigExtensions
    {
        public static BatchLoadConfig RegisterBatchLoad(
            this BatchLoadConfig config,
            PropertyInfo property,
            Type declaringEntityType)
        {
            property.EnsureBatchLoadable(declaringEntityType);

            var canonicalProperty = declaringEntityType.ResolveBatchLoadProperty(
                property.Name) ?? property;
            var key = new BatchLoadPathKey(declaringEntityType, property.Name);

            if (!config.SubBatchLoadConfigs.TryGetValue(key, out var node))
            {
                node = new BatchLoadPathNode(canonicalProperty, declaringEntityType);
                config.SubBatchLoadConfigs[key] = node;
            }

            return node.Children;
        }

        public static BatchLoadConfig GetSubConfig(
            this BatchLoadConfig config,
            PropertyInfo property,
            Type declaringEntityType)
        {
            var key = new BatchLoadPathKey(declaringEntityType, property.Name);
            return config.SubBatchLoadConfigs[key].Children;
        }

        /// <summary>
        /// 将 selection 分析结果注册到目标配置，语义等价于对每条路径调用
        /// <c>.BatchLoad()</c> / <c>.ThenBatchLoad()</c>；已存在的节点不会被覆盖。
        /// </summary>
        public static void ApplySelectionBatchLoad(this BatchLoadConfig target, BatchLoadConfig selectionTree)
        {
            if (selectionTree == null)
            {
                return;
            }

            foreach (var node in selectionTree.SubBatchLoadConfigs.Values)
            {
                var subConfig = target.RegisterBatchLoad(node.Property, node.DeclaringEntityType);
                subConfig.ApplySelectionBatchLoad(node.Children);
            }
        }
    }
}
