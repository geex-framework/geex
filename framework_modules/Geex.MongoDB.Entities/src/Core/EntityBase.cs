using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities
{
    public abstract class EntityBase<T> : IEntityBase where T : class, IEntityBase
    {
        static string UnattachedErrorMessage = $"Cannot perform lazy query for a detached entity: {typeof(T).Name}";
        protected LazyMultiQuery<TEntity, TRelated> ConfigLazyQuery<TEntity, TRelated>(
            Expression<Func<TEntity, IQueryable<TRelated>>> propToLoad, Expression<Func<TRelated, bool>> loadCondition,
            Expression<Func<IQueryable<TEntity>, Expression<Func<TRelated, bool>>>> batchLoadRule,
            Func<IQueryable<TRelated>>? sourceProvider = default) where TRelated : IEntityBase where TEntity : T
        {
            var lazyObj = new LazyMultiQuery<TEntity, TRelated>(loadCondition, batchLoadRule, sourceProvider ??
                                                                           (() => DbContext == null? throw new InvalidOperationException(UnattachedErrorMessage): DbContext.Query<TRelated>()));
            var propertyMember = propToLoad.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            LazyQueryCache[propertyMember.Name] = lazyObj;
            return lazyObj;
        }

        protected LazySingleQuery<TEntity, TRelated> ConfigLazyQuery<TEntity, TRelated>(
            // 关联导航属性
            Expression<Func<TEntity, Lazy<TRelated>>> propExpression,
            // 导航属性的懒加载实现
            Expression<Func<TRelated, bool>> lazyQuery,
            // 导航属性的批量加载实现
            Expression<Func<IQueryable<TEntity>, Expression<Func<TRelated, bool>>>> batchQuery,
            // 批量加载时的数据源, 默认从DbContext查, 可替换成任意第三方Service返回的IQueryable
            Func<IQueryable<TRelated>>? sourceProvider = default) where TRelated : IEntityBase where TEntity : T
        {
            var lazyObj = new LazySingleQuery<TEntity, TRelated>(lazyQuery, batchQuery, sourceProvider ??
                                                                          (() => DbContext == null? throw new InvalidOperationException(UnattachedErrorMessage): DbContext.Query<TRelated>()));
            var propertyMember = propExpression.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            LazyQueryCache[propertyMember.Name] = lazyObj;
            return lazyObj;
        }

        protected LazyMultiQuery<T, TRelated> ConfigLazyQuery<TRelated>(
            Expression<Func<T, IQueryable<TRelated>>> propToLoad, Expression<Func<TRelated, bool>> loadCondition,
            Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batchLoadRule,
            Func<IQueryable<TRelated>>? sourceProvider = default) where TRelated : IEntityBase
        {
            var lazyObj = new LazyMultiQuery<T, TRelated>(loadCondition, batchLoadRule, sourceProvider ??
                                                                           (() => DbContext == null? throw new InvalidOperationException(UnattachedErrorMessage): DbContext.Query<TRelated>()));
            var propertyMember = propToLoad.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            LazyQueryCache[propertyMember.Name] = lazyObj;
            return lazyObj;
        }

        protected LazySingleQuery<T, TRelated> ConfigLazyQuery<TRelated>(
            // 关联导航属性
            Expression<Func<T, Lazy<TRelated>>> propExpression,
            // 导航属性的懒加载实现
            Expression<Func<TRelated, bool>> lazyQuery,
            // 导航属性的批量加载实现
            Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batchQuery,
            // 批量加载时的数据源, 默认从DbContext查, 可替换成任意第三方Service返回的IQueryable
            Func<IQueryable<TRelated>>? sourceProvider = default) where TRelated : IEntityBase
        {
            var lazyObj = new LazySingleQuery<T, TRelated>(lazyQuery, batchQuery, sourceProvider ??
                                                                          (() => DbContext == null? throw new InvalidOperationException(UnattachedErrorMessage): DbContext.Query<TRelated>()));
            var propertyMember = propExpression.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            LazyQueryCache[propertyMember.Name] = lazyObj;
            return lazyObj;
        }

        public EntityBase()
        {

        }
        internal Dictionary<string, ILazyQuery> LazyQueryCache { get; } = new Dictionary<string, ILazyQuery>();

        /// <summary>
        /// 强制转换当前实体为子实体类型, 转换后的实体将保持之前的Attach状态, 转换后的实体与原实体的原始数据相同但类型不同.
        /// </summary>
        /// <typeparam name="TChild"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual TChild CastEntity<TChild>() where TChild : IEntityBase
        {
            try
            {
                if (this is TChild castedEntity)
                {
                    return castedEntity;
                }
                if (this.DbContext != null)
                {
                    var uow = (this.DbContext);
                    uow.Detach(this);
                    var bsonDocument = new BsonDocument();
                    BsonSerializer.LookupSerializer(this.GetType()).Serialize(BsonSerializationContext.CreateRoot(new BsonDocumentWriter(bsonDocument)), this);
                    var child = BsonSerializer.LookupSerializer<TChild>().Deserialize(BsonDeserializationContext.CreateRoot(new BsonDocumentReader(bsonDocument)));
                    return uow.Attach(child);
                }
                else
                {
                    var bsonDocument = new BsonDocument();
                    BsonSerializer.LookupSerializer(this.GetType()).Serialize(BsonSerializationContext.CreateRoot(new BsonDocumentWriter(bsonDocument)), this);
                    var child = BsonSerializer.LookupSerializer<TChild>().Deserialize(BsonDeserializationContext.CreateRoot(new BsonDocumentReader(bsonDocument)));
                    return child;
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"无法将实体 {this.GetType().Name} 转换为子实体 {typeof(TChild).Name}", e);
            }
        }

        protected T LazyQuery<T>(Expression<Func<T>> func, [CallerMemberName] string name = default) where T : class
        {
            var lazyQuery = LazyQueryCache[name];
            if (lazyQuery.Value is T typedValue)
            {
                return typedValue;
            }
            return (dynamic)lazyQuery.Value;
        }
        /// <summary>
        /// This property is auto managed. A new Id will be assigned for new entities upon saving.
        /// </summary>
        [BsonId, ObjectId]
        public string Id { get; [Obsolete("请勿手动设置Id!")] set; }
        DbContext IEntityBase.DbContext { get; set; }

        protected virtual DbContext DbContext
        {
            get => ((IEntityBase)this).DbContext;
            set => ((IEntityBase)this).DbContext = value;
        }
        protected IServiceProvider ServiceProvider => ((IEntityBase)this).DbContext.ServiceProvider;
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }

        /// <summary>
        /// Override this method in order to control the generation of Ids for new entities.
        /// </summary>
        public virtual ObjectId GenerateNewId()
            => ObjectId.GenerateNewId();

        /// <inheritdoc />
        public virtual async Task<long> DeleteAsync(CancellationToken cancellation = default)
        {
            return DbContext != null
                ? await DbContext.DeleteAsync(this as T, cancellation)
                : await DB.DeleteAsync(this.GetType(), this.Id, cancellation: cancellation);
        }

        /// <inheritdoc />
        Dictionary<string, ILazyQuery> IEntityBase.LazyQueryCache => LazyQueryCache;

    }
}
