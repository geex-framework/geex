using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Geex.Notifications;
using Geex.Validation;

using MediatX;

using MongoDB.Entities;

namespace Geex.Storage
{
    public interface IEntity : IEntityBase
    {

        public void AddDomainEvent(params IEvent[] events);
        public Task<ValidationResult> Validate(CancellationToken cancellation = default);
        internal Task ValidateOnAttach();
    }
    public abstract class Entity<T> : EntityBase<T>, IEntity, IHasId where T : class, IEntityBase
    {
        public Entity()
        {
            CreatedOn = DateTimeOffset.MinValue;
            ModifiedOn = DateTimeOffset.MinValue;
        }

        public void AddDomainEvent(params IEvent[] events)
        {
            foreach (var @event in events)
            {
                (this.DbContext as GeexDbContext)?.DomainEvents.Enqueue(@event);
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
        public override async Task<long> DeleteAsync(CancellationToken cancellation = default)
        {
            (this as IEntity)?.AddDomainEvent(new EntityDeletedEvent<T>(this.Id));
            return await base.DeleteAsync(cancellation);
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
