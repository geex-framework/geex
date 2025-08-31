using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities.Core.Comparers;
using MongoDB.Entities.Exceptions;
using MongoDB.Entities.Interceptors;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities
{
    public struct WriteResult
    {
        public static implicit operator WriteResult(UpdateResult result)
        {
            return result.IsAcknowledged switch
            {
                true => new WriteResult()
                {
                    UpsertedId = result.UpsertedId?.ToString(),
                    IsAcknowledged = true,
                    MatchedCount = result.MatchedCount,
                    ModifiedCount = result.ModifiedCount
                },
                _ => new WriteResult() { IsAcknowledged = false }
            };
        }

        public static implicit operator WriteResult(ReplaceOneResult result)
        {
            return result.IsAcknowledged switch
            {
                true => new WriteResult()
                {
                    UpsertedId = result.UpsertedId?.ToString(),
                    IsAcknowledged = true,
                    MatchedCount = result.MatchedCount,
                    ModifiedCount = result.ModifiedCount
                },
                _ => new WriteResult() { IsAcknowledged = false }
            };
        }

        /// <summary>
        /// Gets a value indicating whether the result is acknowledged.
        /// </summary>
        public bool IsAcknowledged { get; init; }

        /// <summary>
        /// Gets a value indicating whether the modified count is available.
        /// </summary>
        /// <remarks>The available modified count.</remarks>
        public bool IsModifiedCountAvailable { get; init; }

        /// <summary>
        /// Gets the matched count. If IsAcknowledged is false, this will throw an exception.
        /// </summary>
        public long MatchedCount { get; init; }

        /// <summary>
        /// Gets the modified count. If IsAcknowledged is false, this will throw an exception.
        /// </summary>
        public long ModifiedCount { get; init; }

        /// <summary>
        /// Gets the upserted id, if one exists. If IsAcknowledged is false, this will throw an exception.
        /// </summary>
        public string? UpsertedId { get; init; }
    }
    public static partial class DB
    {
        private static readonly BulkWriteOptions unOrdBlkOpts = new BulkWriteOptions { IsOrdered = false };
        private static readonly UpdateOptions updateOptions = new UpdateOptions { IsUpsert = true };
        private static readonly ReplaceOptions replaceOptions = new ReplaceOptions() { IsUpsert = true };

        /// <summary>
        /// Saves a complete entity using partial update instead of replace to optimize performance.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="dbContext">An optional DbContext if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static async Task<WriteResult> SaveAsync<T>(T entity, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            var writeModel = await PrepareForSave(entity, dbContext);

            switch (writeModel)
            {
                case UpdateOneModel<T> updateModel:
                    return dbContext?.Session == null
                        ? await Collection<T>().UpdateOneAsync(updateModel.Filter, updateModel.Update, updateOptions, cancellation)
                        : await Collection<T>().UpdateOneAsync(dbContext.Session, updateModel.Filter, updateModel.Update, updateOptions, cancellation);
                case ReplaceOneModel<T> replaceModel:
                    return dbContext?.Session == null
                        ? await Collection<T>().ReplaceOneAsync(replaceModel.Filter, replaceModel.Replacement, replaceOptions, cancellation)
                        : await Collection<T>().ReplaceOneAsync(dbContext.Session, replaceModel.Filter, replaceModel.Replacement, replaceOptions, cancellation);
                default:
                    throw new NotSupportedException("Unsupported write model type: " + writeModel.GetType().Name);
            }
        }

        /// <summary>
        /// Saves a batch of complete entities using partial updates to optimize performance.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="dbContext">An optional DbContext if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static async Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            var updateModels = await PrepareForSaveBatch(entities, dbContext);
            var models = updateModels.ToList();

            return dbContext?.Session == null
                   ? await Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : await Collection<T>().BulkWriteAsync(dbContext.Session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<WriteResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object>> members, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return SavePartial(entity, members, dbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return SavePartial(entities, members, dbContext, cancellation);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<WriteResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object>> members, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return SavePartial(entity, members, dbContext, cancellation, true);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return SavePartial(entities, members, dbContext, cancellation, true);
        }

        private static async Task<WriteModel<T>> PrepareForSave<T>(T entity, DbContext dbContext) where T : IEntityBase
        {
            var now = DateTimeOffset.Now;
            var isNewEntity = false;

            if (dbContext != default && entity.DbContext == default)
            {
                entity = dbContext.Attach(entity);
            }
            else
            {
                if (entity.Id == default)
                {
                    entity.Id = entity.GenerateNewId().ToString();
                    entity.CreatedOn = now;
                    isNewEntity = true;
                }
            }

            if (entity is ISaveIntercepted intercepted)
            {
                var original = dbContext?.DbDataCache[typeof(T)].GetOrDefault(entity.Id);
                await intercepted.InterceptOnSave(original);
            }

            // 在修改ModifiedOn之前进行乐观锁并发检查（仅对已存在的实体）, 忽略 100000 ticks = 10ms 内的修改
            if (!isNewEntity && (DateTime.Now.Ticks - entity.ModifiedOn.Ticks < 100000))
            {
                await CheckOptimisticConcurrency(dbContext, entity);
            }

            entity.ModifiedOn = now;
            var entityType = entity.GetType();
            var typeCache = DB.GetCacheInfo(entityType);
            // 构建更新定义
            var updateDefs = new List<UpdateDefinition<T>>();
            var memberMaps = typeCache.MemberMapsWithoutId;

            foreach (var memberMap in memberMaps)
            {
                var value = memberMap.Getter(entity);
                updateDefs.Add(Builders<T>.Update.Set(memberMap.ElementName, value));
            }

            // 使用完整的继承链判别器数组，如果只有一个判别器则保持向后兼容性
            var discriminators = typeCache.Discriminators;
            var discriminatorValue = discriminators.Count == 1 ? discriminators[0] : (BsonValue)discriminators;
            updateDefs.Add(Builders<T>.Update.Set("_t", discriminatorValue));

            if (!isNewEntity)
            {
                var updateOneModel = new UpdateOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    update: Builders<T>.Update.Combine(updateDefs))
                {
                    IsUpsert = true,
                };

                return updateOneModel;
            }
            else
            {
                var replaceOneModel = new ReplaceOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    replacement: entity)
                {
                    IsUpsert = true,
                };

                return replaceOneModel;
            }
        }

        private static async Task<IEnumerable<WriteModel<T>>> PrepareForSaveBatch<T>(IEnumerable<T> entities, DbContext dbContext) where T : IEntityBase
        {
            var entitiesArray = entities as T[] ?? entities.ToArray();
            if (entitiesArray.Length == 0)
                return Array.Empty<WriteModel<T>>();

            // 按实际类型分组，以减少重复的类型信息获取
            var entitiesByType = entitiesArray.GroupBy(e => e.GetType()).ToArray();
            var models = new List<WriteModel<T>>();

            foreach (var typeGroup in entitiesByType)
            {
                var entityType = typeGroup.Key;
                var entitiesOfSameType = typeGroup.ToArray();

                // 对相同类型的实体批量处理
                var typeModels = await PrepareForSaveBatchActualType<T>(entitiesOfSameType, entityType, dbContext);
                models.AddRange(typeModels);
            }

            return models;
        }

        /// <summary>
        /// 批量处理相同类型的实体，避免重复的类型信息获取和反射操作
        /// </summary>
        /// <typeparam name="T">基础实体类型</typeparam>
        /// <param name="entities">相同类型的实体数组</param>
        /// <param name="entityType">实体的实际类型</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>写入模型集合</returns>
        private static async Task<IEnumerable<WriteModel<T>>> PrepareForSaveBatchActualType<T>(T[] entities, Type entityType, DbContext dbContext) where T : IEntityBase
        {
            var changeSet = new Dictionary<string, DateTimeOffset>(entities.Length);
            var models = new List<WriteModel<T>>(entities.Length);
            var now = DateTimeOffset.Now;

            // 只获取一次类型配置信息
            var typeCache = DB.GetCacheInfo(entityType);
            var discriminators = typeCache.Discriminators;
            var discriminatorValue = discriminators.Count == 1 ? discriminators[0] : (BsonValue)discriminators;

            foreach (var entity in entities)
            {
                var isNewEntity = false;

                if (dbContext != default && entity.DbContext == default)
                {
                    dbContext.Attach(entity);
                }
                else
                {
                    if (entity.Id == default)
                    {
                        entity.Id = entity.GenerateNewId().ToString();
                        entity.CreatedOn = now;
                        isNewEntity = true;
                    }
                }

                if (entity is ISaveIntercepted intercepted)
                {
                    var original = dbContext?.DbDataCache[entityType].GetOrDefault(entity.Id);
                    await intercepted.InterceptOnSave(original);
                }

                // 在修改ModifiedOn之前进行乐观锁并发检查（仅对已存在的实体）, 忽略 100000 ticks = 10ms 内的修改
                if (!isNewEntity && (DateTime.Now.Ticks - entity.ModifiedOn.Ticks < 100000))
                {
                    changeSet.Add(entity.Id, entity.ModifiedOn);
                }

                entity.ModifiedOn = now;

                if (!isNewEntity)
                {
                    // 构建更新定义 - 重用类型配置信息
                    var updateDefs = new List<UpdateDefinition<T>>();
                    var memberMaps = typeCache.MemberMapsWithoutId;

                    foreach (var memberMap in memberMaps)
                    {
                        var value = memberMap.Getter(entity);
                        updateDefs.Add(Builders<T>.Update.Set(memberMap.ElementName, value));
                    }

                    updateDefs.Add(Builders<T>.Update.Set("_t", discriminatorValue));

                    var updateOneModel = new UpdateOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                        update: Builders<T>.Update.Combine(updateDefs))
                    {
                        IsUpsert = true,
                    };

                    models.Add(updateOneModel);
                }
                else
                {
                    var replaceOneModel = new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                        replacement: entity)
                    {
                        IsUpsert = true,
                    };

                    models.Add(replaceOneModel);
                }
            }
            await BulkCheckOptimisticConcurrency<T>(dbContext, changeSet);
            return models;
        }

        private static IEnumerable<string> RootPropNames<T>(Expression<Func<T, object>> members) where T : IEntityBase
        {
            return (members?.Body as NewExpression)?.Arguments
                .Select(a => a.ToString().Split('.')[1]);
        }

        private static async Task<WriteModel<T>> PrepareEntityForPartialSave<T>(T entity, DbContext dbContext,
            Expression<Func<T, object>> members,
            bool excludeMode) where T : IEntityBase
        {
            var propNames = RootPropNames(members);
            if (!propNames.Any())
                throw new ArgumentException("Unable to get any properties from the members expression!");

            var now = DateTimeOffset.Now;
            var isNewEntity = false;

            if (dbContext != default && entity.DbContext == default)
            {
                entity = dbContext.Attach(entity);
            }
            else
            {
                if (entity.Id == default)
                {
                    entity.Id = entity.GenerateNewId().ToString();
                    entity.CreatedOn = now;
                    isNewEntity = true;
                }
            }

            if (entity is ISaveIntercepted intercepted)
            {
                var original = dbContext?.DbDataCache[typeof(T)].GetOrDefault(entity.Id);
                await intercepted.InterceptOnSave(original);
            }

            // 在修改ModifiedOn之前进行乐观锁并发检查（仅对已存在的实体）, 忽略 100000 ticks = 10ms 内的修改
            if (!isNewEntity && (DateTime.Now.Ticks - entity.ModifiedOn.Ticks < 100000))
            {
                await CheckOptimisticConcurrency(dbContext, entity);
            }

            entity.ModifiedOn = now;
            var entityType = entity.GetType();
            var typeCache = DB.GetCacheInfo(entityType);
            // 构建更新定义
            var updateDefs = new List<UpdateDefinition<T>>(50);
            var memberMaps = typeCache.MemberMapsWithoutId.AsEnumerable();

            // 根据成员名称过滤
            if (excludeMode)
                memberMaps = memberMaps.Where(m => !propNames.Contains(m.MemberName));
            else
                memberMaps = memberMaps.Where(m => propNames.Contains(m.MemberName));

            var patchObj = (T)typeof(T).CreateInstanceFast();
            patchObj.Id = entity.Id;
            foreach (var memberMap in memberMaps)
            {
                var value = memberMap.Getter(entity);
                memberMap.Setter(patchObj, value);
                updateDefs.Add(Builders<T>.Update.Set(memberMap.ElementName, value));
            }

            // 使用完整的继承链判别器数组，如果只有一个判别器则保持向后兼容性
            var discriminators = typeCache.Discriminators;
            var discriminatorValue = discriminators.Count == 1 ? discriminators[0] : (BsonValue)discriminators;
            updateDefs.Add(Builders<T>.Update.Set("_t", discriminatorValue));

            if (!isNewEntity)
            {
                var updateOneModel = new UpdateOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    update: Builders<T>.Update.Combine(updateDefs))
                {
                    IsUpsert = true,
                };

                return updateOneModel;
            }
            else
            {
                var replaceOneModel = new ReplaceOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    replacement: patchObj as dynamic)
                {
                    IsUpsert = true,
                };

                return replaceOneModel;
            }
        }

        private static async Task<WriteResult> SavePartial<T>(T entity, Expression<Func<T, object>> members, DbContext dbContext, CancellationToken cancellation, bool excludeMode = false) where T : IEntityBase
        {
            var writeModel = await PrepareEntityForPartialSave(entity, dbContext, members, excludeMode);
            if (writeModel is UpdateOneModel<T> updateOneModel)
            {
                return await Collection<T>().UpdateOneAsync(e => e.Id == entity.Id, updateOneModel.Update, updateOptions, cancellation);
            }
            else if (writeModel is ReplaceOneModel<T> replaceOneModel)
            {
                return await Collection<T>().ReplaceOneAsync(e => e.Id == entity.Id, replaceOneModel.Replacement, replaceOptions, cancellation);
            }

            throw new InvalidOperationException("Invalid write model type");
        }

        /// <summary>
        /// 批量处理相同类型的实体的部分保存，避免重复的类型信息获取和反射操作
        /// </summary>
        /// <typeparam name="T">基础实体类型</typeparam>
        /// <param name="entities">相同类型的实体数组</param>
        /// <param name="entityType">实体的实际类型</param>
        /// <param name="members">要保存/排除的成员表达式</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="excludeMode">是否为排除模式</param>
        /// <returns>写入模型集合</returns>
        private static async Task<IEnumerable<WriteModel<T>>> PrepareForPartialSaveBatchActualType<T>(T[] entities, Type entityType, Expression<Func<T, object>> members, DbContext dbContext, bool excludeMode) where T : IEntityBase
        {
            var propNames = RootPropNames(members);
            if (!propNames.Any())
                throw new ArgumentException("Unable to get any properties from the members expression!");

            var changeSet = new Dictionary<string, DateTimeOffset>(entities.Length);
            var models = new List<WriteModel<T>>(entities.Length);
            var now = DateTimeOffset.Now;

            // 只获取一次类型配置信息
            var typeCache = DB.GetCacheInfo(entityType);
            var discriminators = typeCache.Discriminators;
            var discriminatorValue = discriminators.Count == 1 ? discriminators[0] : (BsonValue)discriminators;

            // 预先计算要处理的成员映射
            var memberMaps = typeCache.MemberMapsWithoutId.AsEnumerable();
            if (excludeMode)
                memberMaps = memberMaps.Where(m => !propNames.Contains(m.MemberName));
            else
                memberMaps = memberMaps.Where(m => propNames.Contains(m.MemberName));

            var memberMapsArray = memberMaps;

            foreach (var entity in entities)
            {
                var isNewEntity = false;

                if (dbContext != default && entity.DbContext == default)
                {
                    dbContext.Attach(entity);
                }
                else
                {
                    if (entity.Id == default)
                    {
                        entity.Id = entity.GenerateNewId().ToString();
                        entity.CreatedOn = now;
                        isNewEntity = true;
                    }
                }

                if (entity is ISaveIntercepted intercepted)
                {
                    var original = dbContext?.DbDataCache[entityType].GetOrDefault(entity.Id);
                    await intercepted.InterceptOnSave(original);
                }

                // 在修改ModifiedOn之前进行乐观锁并发检查（仅对已存在的实体）, 忽略 100000 ticks = 10ms 内的修改
                if (!isNewEntity && (DateTime.Now.Ticks - entity.ModifiedOn.Ticks < 100000))
                {
                    changeSet.Add(entity.Id, entity.ModifiedOn);
                }

                entity.ModifiedOn = now;

                if (!isNewEntity)
                {
                    // 构建更新定义 - 重用类型配置信息和成员映射
                    var updateDefs = new List<UpdateDefinition<T>>(50);

                    foreach (var memberMap in memberMapsArray)
                    {
                        var value = memberMap.Getter(entity);
                        updateDefs.Add(Builders<T>.Update.Set(memberMap.ElementName, value));
                    }

                    updateDefs.Add(Builders<T>.Update.Set("_t", discriminatorValue));

                    var updateOneModel = new UpdateOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                        update: Builders<T>.Update.Combine(updateDefs))
                    {
                        IsUpsert = true,
                    };

                    models.Add(updateOneModel);
                }
                else
                {
                    var patchObj = (T)typeCache.ClassMap.CreateInstance();
                    patchObj.Id = entity.Id;
                    foreach (var memberMap in memberMapsArray)
                    {
                        var value = memberMap.Getter(entity);
                        memberMap.Setter(patchObj, value);
                    }

                    var replaceOneModel = new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                        replacement: patchObj as dynamic)
                    {
                        IsUpsert = true,
                    };

                    models.Add(replaceOneModel);
                }
            }
            await BulkCheckOptimisticConcurrency<T>(dbContext, changeSet);
            return models;
        }

        private static async Task<BulkWriteResult<T>> SavePartial<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, DbContext dbContext, CancellationToken cancellation, bool excludeMode = false) where T : IEntityBase
        {
            var entitiesArray = entities as T[] ?? entities.ToArray();
            if (entitiesArray.Length == 0)
                return new BulkWriteResult<T>.Acknowledged(0, 0, 0, 0, null, [], []);

            // 按实际类型分组，以减少重复的类型信息获取
            var entitiesByType = entitiesArray.GroupBy(e => e.GetType()).ToArray();
            var models = new List<WriteModel<T>>();

            foreach (var typeGroup in entitiesByType)
            {
                var entityType = typeGroup.Key;
                var entitiesOfSameType = typeGroup.ToArray();

                // 对相同类型的实体批量处理
                var typeModels = await PrepareForPartialSaveBatchActualType<T>(entitiesOfSameType, entityType, members, dbContext, excludeMode);
                models.AddRange(typeModels);
            }

            return dbContext?.Session == null
                ? await Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                : await Collection<T>().BulkWriteAsync(dbContext.Session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// 乐观锁并发检查
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        private static async Task BulkCheckOptimisticConcurrency<T>(DbContext dbContext, Dictionary<string, DateTimeOffset> changeSet) where T : IEntityBase
        {
            if (dbContext == null)
            {
                return;
            }

            if (!changeSet.Any())
            {
                return;
            }

            var rootType = DB.GetCacheInfo(typeof(T)).RootEntityType;
            var ids = dbContext.DbDataCache[rootType].Keys.Except(changeSet.Keys).ToArray();
            if (!ids.Any())
            {
                return;
            }
            var dbRecents = await Collection<T>().FindSync(Builders<T>.Filter.In(x => x.Id, ids)).ToListAsync();
            for (var i = 0; i < dbRecents.Count; i++)
            {
                var dbRecent = dbRecents[i];
                // 查询数据库获取最新值来检查是否有并发修改
                if (dbRecent == null)
                {
                    // 有字段冲突，抛出乐观锁异常
                    dbContext.Logger.LogError(
                        "Optimistic concurrency conflict, entity removed: {EntityType}[{EntityId}]",
                        rootType.Name, ids[i]);

                    throw new OptimisticConcurrencyException(
                        rootType,
                        dbRecent.Id,
                        [],
                        default,
                        default
                    );
                }

                // 检查ModifiedOn是否发生了变化（乐观锁冲突检测）
                var changedItemDate = changeSet[dbRecent.Id];
                var existingDbValue = dbContext.DbDataCache[rootType][dbRecent.Id];
                if (dbRecent.ModifiedOn <= changedItemDate)
                {
                    // 没有冲突，数据库值没有更新
                    return;
                }

                dbContext.Logger.LogDebug("ModifiedOn changed for {EntityType}[{EntityId}]", rootType.Name,
                    dbRecent.Id);

                // 检查本地内存中是否有该实体的修改
                if (!dbContext.MemoryDataCache[rootType].TryGetValue(dbRecent.Id, out var localValue))
                {
                    // 本地没有该实体，更新缓存并继续
                    dbContext.Logger.LogWarning("Entity {EntityType}[{EntityId}] updated in DB but not in local cache",
                        rootType.Name, dbRecent.Id);
                    return;
                }

                // 比较本地值是否有修改
                var ourChanges = dbContext.Diff(localValue, existingDbValue, BsonDiffMode.Full);
                if (ourChanges.AreEqual)
                {
                    // 本地值没有修改，直接更新缓存
                    dbContext.Logger.LogWarning("Entity {EntityType}[{EntityId}] updated in DB, local has no changes",
                        rootType.Name, dbRecent.Id);
                    return;
                }

                // 本地值有修改，检查字段冲突
                var theirChanges = dbContext.Diff(dbRecent, existingDbValue, BsonDiffMode.Full);
                var conflictingFields =
                    DetectConflictingFields(ourChanges, theirChanges, existingDbValue, localValue, dbRecent);

                if (conflictingFields.Count > 0)
                {
                    // 有字段冲突，抛出乐观锁异常
                    dbContext.Logger.LogError(
                        "Optimistic concurrency conflict: {EntityType}[{EntityId}], fields: {ConflictingFields}",
                        rootType.Name, dbRecent.Id, string.Join(", ", conflictingFields.Select(f => f.FieldName)));

                    throw new OptimisticConcurrencyException(
                        rootType,
                        dbRecent.Id,
                        conflictingFields,
                        existingDbValue.ModifiedOn,
                        dbRecent.ModifiedOn
                    );
                }
                else
                {
                    // 没有字段冲突，记录信息日志
                    dbContext.Logger.LogInformation(
                        "Concurrent modifications without field conflicts: {EntityType}[{EntityId}]", rootType.Name,
                        dbRecent.Id);
                }
            }
        }

        /// <summary>
        /// 乐观锁并发检查
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="entity">当前实体</param>
        /// <param name="entityModifiedOn"></param>
        private static async Task CheckOptimisticConcurrency<T>(DbContext dbContext, T entity) where T : IEntityBase
        {
            // 如果没有DbContext或者实体ID为空，跳过乐观锁检查
            if (dbContext == null || string.IsNullOrEmpty(entity.Id))
            {
                return;
            }

            var rootType = DB.GetCacheInfo(typeof(T)).RootEntityType;

            // 获取缓存中的数据库值
            var existingDbValue = dbContext.DbDataCache[rootType].GetOrDefault(entity.Id);
            if (existingDbValue == null)
            {
                // 数据库中没有此实体，跳过检查
                return;
            }
            // 查询数据库获取最新值来检查是否有并发修改
            var currentDbValue = Collection<T>().FindSync(Builders<T>.Filter.Eq(x => x.Id, entity.Id)).FirstOrDefault();
            if (currentDbValue == null)
            {
                // 实体已被删除
                return;
            }

            // 检查ModifiedOn是否发生了变化（乐观锁冲突检测）
            if (currentDbValue.ModifiedOn <= existingDbValue.ModifiedOn)
            {
                // 没有冲突，数据库值没有更新
                return;
            }

            dbContext.Logger.LogDebug("ModifiedOn changed for {EntityType}[{EntityId}]", rootType.Name, currentDbValue.Id);

            // 检查本地内存中是否有该实体的修改
            if (!dbContext.MemoryDataCache[rootType].TryGetValue(entity.Id, out var localValue))
            {
                // 本地没有该实体，更新缓存并继续
                dbContext.Logger.LogWarning("Entity {EntityType}[{EntityId}] updated in DB but not in local cache", rootType.Name, entity.Id);
                return;
            }

            // 比较本地值是否有修改
            var ourChanges = dbContext.Diff(localValue, existingDbValue, BsonDiffMode.Full);
            if (ourChanges.AreEqual)
            {
                // 本地值没有修改，直接更新缓存
                dbContext.Logger.LogWarning("Entity {EntityType}[{EntityId}] updated in DB, local has no changes", rootType.Name, entity.Id);
                return;
            }

            // 本地值有修改，检查字段冲突
            var theirChanges = dbContext.Diff(currentDbValue, existingDbValue, BsonDiffMode.Full);
            var conflictingFields = DetectConflictingFields(ourChanges, theirChanges, existingDbValue, localValue, currentDbValue);

            if (conflictingFields.Count > 0)
            {
                // 有字段冲突，抛出乐观锁异常
                dbContext.Logger.LogError("Optimistic concurrency conflict: {EntityType}[{EntityId}], fields: {ConflictingFields}",
                                         rootType.Name, currentDbValue.Id, string.Join(", ", conflictingFields.Select(f => f.FieldName)));

                throw new OptimisticConcurrencyException(
                    rootType,
                    currentDbValue.Id,
                    conflictingFields,
                    existingDbValue.ModifiedOn,
                    currentDbValue.ModifiedOn
                    );
            }
            else
            {
                // 没有字段冲突，记录信息日志
                dbContext.Logger.LogInformation("Concurrent modifications without field conflicts: {EntityType}[{EntityId}]", rootType.Name, entity.Id);
            }
        }

        /// <summary>
        /// 检测字段冲突并构建冲突信息
        /// </summary>
        /// <param name="ourChanges">我们的变更</param>
        /// <param name="theirChanges">他们的变更</param>
        /// <param name="baseValue">基础值</param>
        /// <param name="ourValue">我们的值</param>
        /// <param name="theirValue">他们的值</param>
        /// <returns>冲突字段的详细信息列表</returns>
        private static List<FieldConflictInfo> DetectConflictingFields(
            BsonDiffResult ourChanges,
            BsonDiffResult theirChanges,
            IEntityBase baseValue,
            IEntityBase ourValue,
            IEntityBase theirValue)
        {
            var conflictingFields = new List<FieldConflictInfo>();

            if (ourChanges.Differences == null || theirChanges.Differences == null)
            {
                return conflictingFields;
            }

            // 检查我们修改的字段是否与他们修改的字段有交集
            var ourModifiedFields = ourChanges.Differences.Keys;
            var theirModifiedFields = theirChanges.Differences.Keys;

            foreach (var fieldName in ourModifiedFields)
            {
                if (theirModifiedFields.Contains(fieldName))
                {
                    var ourDiff = ourChanges.Differences[fieldName];
                    var theirDiff = theirChanges.Differences[fieldName];

                    // 构建字段冲突信息
                    var conflictInfo = new FieldConflictInfo
                    {
                        FieldName = fieldName,
                        FieldType = ourDiff.FieldType,
                        BaseValue = GetFieldValue(baseValue, fieldName, ourDiff.FieldType),
                        OurValue = GetFieldValue(ourValue, fieldName, ourDiff.FieldType),
                        TheirValue = GetFieldValue(theirValue, fieldName, theirDiff.FieldType)
                    };

                    conflictingFields.Add(conflictInfo);
                }
            }

            return conflictingFields;
        }

        /// <summary>
        /// 通过反射获取字段值
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="fieldType">字段类型</param>
        /// <returns>字段值</returns>
        private static object GetFieldValue(IEntityBase entity, string fieldName, Type fieldType)
        {
            if (entity == null) return null;

            try
            {
                var property = entity.GetType().GetProperty(fieldName);
                if (property != null)
                {
                    return property.GetValue(entity);
                }

                var field = entity.GetType().GetField(fieldName);
                if (field != null)
                {
                    return field.GetValue(entity);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
