using System;
using System.Collections;
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
            WriteResult result = writeModel switch
            {
                UpdateOneModel<T> updateModel => dbContext?.Session == null
                    ? await Collection<T>().UpdateOneAsync(updateModel.Filter, updateModel.Update, updateOptions, cancellation)
                    : await Collection<T>().UpdateOneAsync(dbContext.Session, updateModel.Filter, updateModel.Update, updateOptions, cancellation),
                ReplaceOneModel<T> replaceModel => dbContext?.Session == null
                    ? await Collection<T>().ReplaceOneAsync(replaceModel.Filter, replaceModel.Replacement, replaceOptions, cancellation)
                    : await Collection<T>().ReplaceOneAsync(dbContext.Session, replaceModel.Filter, replaceModel.Replacement, replaceOptions, cancellation),
                _ => throw new NotSupportedException("Unsupported write model type: " + writeModel.GetType().Name)
            };
            dbContext?.UpdateDbDataCache(entity);
            return result;
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
            // 按实际类型分组，以减少重复的类型信息获取
            var models = entities is ICollection<T> collection ? new List<WriteModel<T>>(collection.Count) : new List<WriteModel<T>>(64);
            var entitiesByType = entities.GroupBy(e => e.GetType());

            foreach (var typeGroup in entitiesByType)
            {
                var entityType = typeGroup.Key;
                var entitiesOfSameType = typeGroup;

                // 对相同类型的实体批量处理
                var typeModels = await PrepareForSaveBatchActualType<T>(entitiesOfSameType, entityType, dbContext);
                models.AddRange(typeModels);
            }

            if (models.Count == 0)
            {
                return default;
            }

            var result = dbContext?.Session == null
                   ? await Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : await Collection<T>().BulkWriteAsync(dbContext.Session, models, unOrdBlkOpts, cancellation);
            dbContext?.UpdateDbDataCache(entities);
            return result;
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
            var isNewEntity = entity.ModifiedOn == default;

            if (dbContext != default && entity.DbContext == default)
            {
                dbContext.Attach(entity);
            }
            if (entity.Id == null)
            {
                entity.Id = entity.GenerateNewId().ToString();
                entity.CreatedOn = DateTimeOffset.Now;
            }

            var entityType = entity.GetType();
            var typeCache = DB.GetCacheInfo(entityType).ToTyped<T>();

            var snapshot = dbContext?.DbDataCache[typeCache.RootEntityType].GetOrDefault(entity.Id);
            if (entity is ISaveIntercepted intercepted)
            {
                await intercepted.InterceptOnSave(snapshot);
            }


            if (!isNewEntity)
            {
                var bsonDiffResult = await CheckChanges<T>(typeCache, dbContext, snapshot, entity, typeCache.MemberMapsWithoutSpecial);

                if (bsonDiffResult.AreEqual)
                {
                    return null;
                }

                // 构建更新定义
                var differences = bsonDiffResult.Differences;
                var updateDefs = new List<UpdateDefinition<T>>(differences.Count);

                foreach (var (propName, fieldDiff) in differences)
                {
                    var value = fieldDiff.NewValue;
                    updateDefs.Add(Builders<T>.Update.Set(propName, value));
                }

                var discriminators = typeCache.Discriminators;
                updateDefs.Add(Builders<T>.Update.Set("_t", discriminators));
                updateDefs.Add(Builders<T>.Update.Set(nameof(IEntityBase.ModifiedOn), DateTimeOffset.Now));
                return new UpdateOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    update: Builders<T>.Update.Combine(updateDefs))
                {
                    IsUpsert = true,
                };
            }
            else
            {
                entity.ModifiedOn = DateTimeOffset.Now;
                return new ReplaceOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    replacement: entity)
                {
                    IsUpsert = true,
                };
            }
        }

        /// <summary>
        /// 批量处理相同类型的实体，避免重复的类型信息获取和反射操作
        /// </summary>
        /// <typeparam name="T">基础实体类型</typeparam>
        /// <param name="entities">相同类型的实体数组</param>
        /// <param name="entityType">实体的实际类型</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>写入模型集合</returns>
        private static async Task<IEnumerable<WriteModel<T>>> PrepareForSaveBatchActualType<T>(IEnumerable<T> entities, Type entityType, DbContext dbContext) where T : IEntityBase
        {
            var models = entities is ICollection<T> collection ? new List<WriteModel<T>>(collection.Count) : new List<WriteModel<T>>(64);

            // 只获取一次类型配置信息
            var typeCache = DB.GetCacheInfo(entityType).ToTyped<T>();
            var rootType = typeCache.RootEntityType;

            foreach (var entity in entities)
            {
                var isNewEntity = entity.ModifiedOn == default;

                if (dbContext != default && entity.DbContext == default)
                {
                    dbContext.Attach(entity);
                }
                if (entity.Id == null)
                {
                    entity.Id = entity.GenerateNewId().ToString();
                    entity.CreatedOn = DateTimeOffset.Now;
                }

                var snapshot = dbContext?.DbDataCache[rootType].GetOrDefault(entity.Id);
                if (entity is ISaveIntercepted intercepted)
                {
                    await intercepted.InterceptOnSave(snapshot);
                }

                // 设置ModifiedOn时间
                if (!isNewEntity)
                {
                    var bsonDiffResult = await CheckChanges<T>(typeCache, dbContext, snapshot, entity, typeCache.MemberMapsWithoutSpecial);

                    if (bsonDiffResult.AreEqual)
                    {
                        continue;
                    }

                    // 构建更新定义
                    var differences = bsonDiffResult.Differences;
                    var updateDefs = new List<UpdateDefinition<T>>(differences.Count);

                    foreach (var (propName, fieldDiff) in differences)
                    {
                        var value = fieldDiff.NewValue;
                        updateDefs.Add(Builders<T>.Update.Set(propName, value));
                    }

                    var discriminators = typeCache.Discriminators;
                    updateDefs.Add(Builders<T>.Update.Set("_t", discriminators));
                    updateDefs.Add(Builders<T>.Update.Set(nameof(IEntityBase.ModifiedOn), DateTimeOffset.Now));
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
                    entity.ModifiedOn = DateTimeOffset.Now;
                    var replaceOneModel = new ReplaceOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    replacement: entity)
                    {
                        IsUpsert = true,
                    };

                    models.Add(replaceOneModel);
                }
            }

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

            var isNewEntity = entity.ModifiedOn == default;

            if (dbContext != default && entity.DbContext == default)
            {
                dbContext.Attach(entity);
            }
            if (entity.Id == null)
            {
                entity.Id = entity.GenerateNewId().ToString();
                entity.CreatedOn = DateTimeOffset.Now;
            }

            var typeCache = DB.GetCacheInfo(typeof(T)).ToTyped<T>();
            var rootType = typeCache.RootEntityType;
            var snapshot = dbContext?.DbDataCache[rootType].GetOrDefault(entity.Id);
            if (entity is ISaveIntercepted intercepted)
            {
                await intercepted.InterceptOnSave(snapshot);
            }
            var memberMaps = typeCache.MemberMapsWithoutSpecial.AsEnumerable();
            // 根据成员名称过滤
            if (excludeMode)
                memberMaps = memberMaps.Where(m => !propNames.Contains(m.MemberName));
            else
                memberMaps = memberMaps.Where(m => propNames.Contains(m.MemberName));

            if (!isNewEntity)
            {
                var bsonDiffResult = await CheckChanges<T>(typeCache, dbContext, snapshot, entity, memberMaps);

                if (bsonDiffResult.AreEqual)
                {
                    return null;
                }

                // 构建更新定义
                var differences = bsonDiffResult.Differences;
                var updateDefs = new List<UpdateDefinition<T>>(differences.Count);

                foreach (var (propName, fieldDiff) in differences)
                {
                    var value = fieldDiff.NewValue;
                    updateDefs.Add(Builders<T>.Update.Set(propName, value));
                }

                var discriminators = typeCache.Discriminators;
                updateDefs.Add(Builders<T>.Update.Set("_t", discriminators));
                updateDefs.Add(Builders<T>.Update.Set(nameof(IEntityBase.ModifiedOn), DateTimeOffset.Now));
                return new UpdateOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    update: Builders<T>.Update.Combine(updateDefs))
                {
                    IsUpsert = true,
                };
            }
            else
            {
                var patchObj = (T)typeCache.ClassMap.CreateInstance();
                patchObj.Id = entity.Id;
                patchObj.ModifiedOn = DateTimeOffset.Now;
                foreach (var memberMap in memberMaps)
                {
                    var value = memberMap.Getter(entity);
                    memberMap.Setter(patchObj, value);
                }

                return new ReplaceOneModel<T>(
                    filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                    replacement: patchObj)
                {
                    IsUpsert = true,
                };
            }
        }

        private static async Task<WriteResult> SavePartial<T>(T entity, Expression<Func<T, object>> members, DbContext dbContext, CancellationToken cancellation, bool excludeMode = false) where T : IEntityBase
        {
            var writeModel = await PrepareEntityForPartialSave(entity, dbContext, members, excludeMode);
            WriteResult result = writeModel switch
            {
                UpdateOneModel<T> updateOneModel =>
                    await Collection<T>().UpdateOneAsync(e => e.Id == entity.Id, updateOneModel.Update, updateOptions, cancellation),
                ReplaceOneModel<T> replaceOneModel =>
                    await Collection<T>().ReplaceOneAsync(e => e.Id == entity.Id, replaceOneModel.Replacement, replaceOptions, cancellation),
                _ => throw new InvalidOperationException("Invalid write model type")
            };

            dbContext?.UpdateDbDataCache(entity);
            return result;
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
        private static async Task<IEnumerable<WriteModel<T>>> PrepareForPartialSaveBatchActualType<T>(IEnumerable<T> entities, Type entityType, Expression<Func<T, object>> members, DbContext dbContext, bool excludeMode) where T : IEntityBase
        {
            var propNames = RootPropNames(members);
            if (!propNames.Any())
                throw new ArgumentException("Unable to get any properties from the members expression!");
            var models = entities is ICollection<T> collection ? new List<WriteModel<T>>(collection.Count) : new List<WriteModel<T>>(64);

            // 只获取一次类型配置信息
            var typeCache = DB.GetCacheInfo(entityType).ToTyped<T>();
            var rootType = typeCache.RootEntityType;
            // 预先计算要处理的成员映射
            var memberMaps = typeCache.MemberMapsWithoutSpecial.AsEnumerable();
            if (excludeMode)
                memberMaps = memberMaps.Where(m => !propNames.Contains(m.MemberName));
            else
                memberMaps = memberMaps.Where(m => propNames.Contains(m.MemberName));

            foreach (var entity in entities)
            {
                var isNewEntity = entity.ModifiedOn == default;

                if (dbContext != default && entity.DbContext == default)
                {
                    dbContext.Attach(entity);
                }
                if (entity.Id == null)
                {
                    entity.Id = entity.GenerateNewId().ToString();
                    entity.CreatedOn = DateTimeOffset.Now;
                }

                var snapshot = dbContext?.DbDataCache[rootType].GetOrDefault(entity.Id);
                if (entity is ISaveIntercepted intercepted)
                {
                    await intercepted.InterceptOnSave(snapshot);
                }

                // 设置ModifiedOn时间
                if (!isNewEntity)
                {
                    var bsonDiffResult = await CheckChanges<T>(typeCache, dbContext, snapshot, entity, memberMaps);

                    if (bsonDiffResult.AreEqual)
                    {
                        continue;
                    }

                    // 构建更新定义
                    var differences = bsonDiffResult.Differences;
                    var updateDefs = new List<UpdateDefinition<T>>(differences.Count);

                    foreach (var (propName, fieldDiff) in differences)
                    {
                        var value = fieldDiff.NewValue;
                        updateDefs.Add(Builders<T>.Update.Set(propName, value));
                    }

                    var discriminators = typeCache.Discriminators;
                    updateDefs.Add(Builders<T>.Update.Set("_t", discriminators));
                    updateDefs.Add(Builders<T>.Update.Set(nameof(IEntityBase.ModifiedOn), DateTimeOffset.Now));
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
                    foreach (var memberMap in memberMaps)
                    {
                        var value = memberMap.Getter(entity);
                        memberMap.Setter(patchObj, value);
                    }
                    patchObj.ModifiedOn = DateTimeOffset.Now;
                    var replaceOneModel = new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, entity.Id),
                        replacement: patchObj)
                    {
                        IsUpsert = true,
                    };

                    models.Add(replaceOneModel);
                }
            }

            return models;
        }

        private static async Task<BulkWriteResult<T>> SavePartial<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, DbContext dbContext, CancellationToken cancellation, bool excludeMode = false) where T : IEntityBase
        {
            // 按实际类型分组，以减少重复的类型信息获取
            var entitiesByType = entities.GroupBy(e => e.GetType());
            var models = entities is ICollection<T> collection ? new List<WriteModel<T>>(collection.Count) : new List<WriteModel<T>>(64);

            foreach (var typeGroup in entitiesByType)
            {
                var entityType = typeGroup.Key;
                var entitiesOfSameType = typeGroup;

                // 对相同类型的实体批量处理
                var typeModels = await PrepareForPartialSaveBatchActualType<T>(entitiesOfSameType, entityType, members, dbContext, excludeMode);
                models.AddRange(typeModels);
            }

            if (models.Count == 0)
            {
                return default;
            }

            var result = dbContext?.Session == null
                ? await Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                : await Collection<T>().BulkWriteAsync(dbContext.Session, models, unOrdBlkOpts, cancellation);
            dbContext?.UpdateDbDataCache(entities);
            return result;
        }

        /// <summary>
        /// 批量乐观锁并发检查和自动合并, 必须在ModifiedOn被更新前调用
        /// </summary>
        /// <param name="typeCache"></param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="ourEntity"></param>
        /// <param name="memberMapsToUpdate"></param>
        /// <returns>可以自动合并的变更字典，键为实体ID，值为他们的变更</returns>
        private static async Task<BsonDiffResult> CheckChanges<T>(CacheInfo<T> typeCache, DbContext? dbContext,
            IEntityBase snapshot, T ourEntity, IEnumerable<BsonMemberMap> memberMapsToUpdate) where T : IEntityBase
        {
            var rootType = typeCache.RootEntityType;

            var ourChanges = BsonDataDiffer.DiffEntity(typeCache, snapshot, ourEntity, BsonDiffMode.Full, memberMapsToUpdate);

            if (ourChanges.AreEqual || snapshot == null)
            {
                return ourChanges;
            }

            var filter = Builders<T>.Filter.Eq(x => x.Id, ourEntity.Id);
            // 第一阶段：只查询Id和ModifiedOn字段，识别真正有并发修改的实体
            var touch = Collection<T>()
                .Find(filter)
                .Project(typeCache.ProjectionIdAndModifiedOn)
                .FirstOrDefault();

            if (touch == null)
            {
                throw new OptimisticConcurrencyException(
                    rootType,
                    ourEntity.Id,
                    [],
                    snapshot.ModifiedOn,
                    null
                );
            }

            var currentDbTimestamp = touch[nameof(IEntityBase.ModifiedOn)].ToUniversalTime();


            // 如果没有并发修改，直接返回
            if (currentDbTimestamp <= snapshot.ModifiedOn)
            {
                return ourChanges;
            }

            var theirEntity = await Collection<T>().Find(filter).FirstOrDefaultAsync();
            var theirChanges = BsonDataDiffer.DiffEntity(typeCache, snapshot, theirEntity, mode: BsonDiffMode.Full, memberMapsToCompare: memberMapsToUpdate);

            if (theirChanges.AreEqual)
            {
                return ourChanges;
            }

            // 检查冲突
            var conflictingFields = DetectConflictingFields(ourChanges, theirChanges);

            if (conflictingFields.Count > 0)
            {
                throw new OptimisticConcurrencyException(
                    rootType,
                    ourEntity.Id,
                    conflictingFields,
                    snapshot.ModifiedOn,
                    theirEntity.ModifiedOn
                );
            }

            return ourChanges;
        }

        /// <summary>
        /// 检测字段冲突并构建冲突信息
        /// </summary>
        /// <param name="ourChanges">我们的变更</param>
        /// <param name="theirChanges">他们的变更</param>
        /// <returns>冲突字段的详细信息列表</returns>
        private static List<FieldConflictInfo> DetectConflictingFields(
            BsonDiffResult ourChanges,
            BsonDiffResult theirChanges)
        {
            var conflictingFields = new List<FieldConflictInfo>();

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
                        BaseValue = ourDiff.BaseValue,
                        OurValue = ourDiff.NewValue,
                        TheirValue = theirDiff.NewValue
                    };

                    if (!BsonDataDiffer.AreValuesEqual(conflictInfo.OurValue, conflictInfo.TheirValue, ourDiff.FieldType, theirDiff.FieldType))
                    {
                        conflictingFields.Add(conflictInfo);
                    }
                }
            }

            return conflictingFields;
        }
    }
}
