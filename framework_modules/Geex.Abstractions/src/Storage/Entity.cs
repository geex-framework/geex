using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using MediatX;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

using SharpCompress.Writers;

namespace Geex.Storage
{
    public interface IEntity : IEntityBase
    {

        public void AddDomainEvent(params IEvent[] events);
        public Task<ValidationResult> Validate(CancellationToken cancellation = default);
        internal Task ValidateOnAttach();
    }
    public abstract class Entity<T> : EntityBase<T>, IEntity, IModifiedOn, IHasId where T : IEntityBase
    {
        public DateTimeOffset ModifiedOn { get; set; }

        public void AddDomainEvent(params IEvent[] events)
        {
            foreach (var @event in events)
            {
                (this.DbContext as GeexDbContext)?.DomainEvents.Enqueue(@event);
            }
        }

        public virtual TChild ConvertToChild<TChild>() where TChild : T
        {
            try
            {
                var uow = (this.DbContext as GeexDbContext);
                uow.Detach(this);
                var bsonDocument = new BsonDocument();
                BsonSerializer.LookupSerializer(this.GetType()).Serialize(BsonSerializationContext.CreateRoot(new BsonDocumentWriter(bsonDocument)), this);
                //var obj = BsonTypeMapper.MapToDotNetValue(bsonDocument);
                //JsonSerializer.Serialize(writer, obj, options);
                var child = BsonSerializer.LookupSerializer<TChild>().Deserialize(BsonDeserializationContext.CreateRoot(new BsonDocumentReader(bsonDocument)));
                //var child = ActivatorUtilities.CreateInstance<TChild>(uow.ServiceProvider, this, uow);
                return uow.Attach(child);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"无法将实体 {this.GetType().Name} 转换为子实体 {typeof(TChild).Name}, 子实体必须具有`public TChild(Parent parent, IUnitOfWork uow = default){{/* ... */}}`的构造函数", e);
            }
        }

        /// <inheritdoc />
        protected IUnitOfWork Uow => base.DbContext as IUnitOfWork;

        /// <summary>校验对象合法性, 会在对象被Attach后立即触发</summary>
        /// <returns>A collection that holds failed-validation information.</returns>
        public virtual Task<ValidationResult> Validate(CancellationToken cancellation = default)
        {
            return Task.FromResult(ValidationResult.Success);
        }

        /// <inheritdoc />
        async Task IEntity.ValidateOnAttach()
        {
            var validationResult = (await this.Validate(CancellationToken.None));
            if (validationResult != ValidationResult.Success)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, null, $"{validationResult.ErrorMessage}{Environment.NewLine}{validationResult.MemberNames.JoinAsString(",")}");
            }
        }
    }
}
