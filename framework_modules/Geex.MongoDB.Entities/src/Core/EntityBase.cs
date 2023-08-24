using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MethodTimer;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Entities.Utilities;

using SharpCompress.Common;

namespace MongoDB.Entities
{
    public abstract class EntityBase<T> : IEntityBase where T : IEntityBase
    {
        protected LazyMultiQuery<T, TRelated> ConfigLazyQuery<TRelated>(
            Expression<Func<T, IQueryable<TRelated>>> propExpression, Expression<Func<TRelated, bool>> lazyQuery,
            Expression<Func<IQueryable<T>, Expression<Func<TRelated, bool>>>> batchQuery,
            Func<IQueryable<TRelated>> sourceProvider = default) where TRelated : IEntityBase
        {
            var lazyObj = new LazyMultiQuery<T, TRelated>(lazyQuery, batchQuery, sourceProvider ??
                                                                           (() => DbContext.Query<TRelated>()));
            var propertyMember = propExpression.Body.As<MemberExpression>().Member.As<PropertyInfo>();
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
            Func<IQueryable<TRelated>> sourceProvider = default) where TRelated : IEntityBase
        {
            var lazyObj = new LazySingleQuery<T, TRelated>(lazyQuery, batchQuery, sourceProvider ??
                                                                          (() => DbContext.Query<TRelated>()));
            var propertyMember = propExpression.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            LazyQueryCache[propertyMember.Name] = lazyObj;
            return lazyObj;
        }

        public EntityBase()
        {

        }
        internal Dictionary<string, ILazyQuery> LazyQueryCache { get; } = new Dictionary<string, ILazyQuery>();

        ///// <inheritdoc />
        //ILazyQuery IEntity.ConfigLazyQueryable(Expression lazyQuery, Expression batchQuery, Func<IQueryable> sourceProvider)
        //{
        //    if (lazyQuery is object)
        //    {

        //    }
        //    //var lazyObj = new LazyMultiQuery<T, TRelated>(lazyQuery, batchQuery, sourceProvider ??
        //    //                                                               (() => DbContext.Queryable<TRelated>()));
        //    //var lazyObj = new LazySingleQuery<T, TRelated>(lazyQuery, batchQuery, sourceProvider ??
        //    //                                                             (() => DbContext.Queryable<TRelated>()));
        //    //var propertyMember = propExpression.Body.As<MemberExpression>().Member.As<PropertyInfo>();
        //    //LazyQueryCache[propertyMember.Name] = lazyObj;
        //    //return lazyObj;
        //    return default;
        //}

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

        /// <summary>
        /// Override this method in order to control the generation of Ids for new entities.
        /// </summary>
        public virtual ObjectId GenerateNewId()
            => ObjectId.GenerateNewId();

        /// <inheritdoc />
        public virtual async Task<DeleteResult> DeleteAsync()
        {
            if (DbContext != default)
            {
                return await this.DbContext.DeleteAsync(this);
            }
            return await DB.DeleteAsync(this.GetType(), this.Id);
        }

        /// <inheritdoc />
        Dictionary<string, ILazyQuery> IEntityBase.LazyQueryCache => LazyQueryCache;
    }
}
