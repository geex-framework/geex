using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using MongoDB.Entities;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadExtensions
    {
        extension(IQueryable queryable)
        {
            public void MergeBatchLoadConfig(BatchLoadConfig config) =>
                BatchLoadHelper.MergeConfig(queryable, config);

            public void BatchLoadLazyQueries(BatchLoadConfig config) =>
                BatchLoadHelper.BatchLoadLazyQueries(queryable, config);
        }

        extension(IEntityBase entity)
        {
            public void LoadBatchLoad(BatchLoadConfig config) =>
                BatchLoadHelper.LoadEntities([entity], config);
        }

        extension(IEnumerable<IEntityBase> entities)
        {
            public void LoadBatchLoad(BatchLoadConfig config) =>
                BatchLoadHelper.LoadEntities(entities, config);
        }

        extension(BatchLoadConfig config)
        {
            public int SubBatchLoadConfigCount => config.SubBatchLoadConfigs.Count;

            public BatchLoadConfig GetOrAddSubConfig(PropertyInfo property)
            {
                if (!config.SubBatchLoadConfigs.TryGetValue(property, out var subConfig))
                {
                    subConfig = new BatchLoadConfig();
                    config.SubBatchLoadConfigs[property] = subConfig;
                }

                return subConfig;
            }

            public bool ContainsSubConfig(PropertyInfo property) =>
                config.SubBatchLoadConfigs.ContainsKey(property);

            public BatchLoadConfig EnsurePath(IReadOnlyList<PropertyInfo> path) =>
                BatchLoadHelper.EnsurePath(config, path);
        }

        extension(PropertyInfo property)
        {
            public bool IsLazyQueryNavigationProperty() =>
                BatchLoadHelper.IsLazyQueryNavigationProperty(property);

            public bool TryGetRelatedEntityType(out Type relatedEntityType) =>
                BatchLoadHelper.TryGetRelatedEntityType(property, out relatedEntityType);
        }
    }
}
