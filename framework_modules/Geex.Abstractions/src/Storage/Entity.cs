using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using MediatR;
using MongoDB.Entities;

namespace Geex.Abstractions.Storage
{
    public interface IEntity
    {

        public void AddDomainEvent(params INotification[] events);
        public Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default);
         /// <summary>
        /// 用于校验对象的合法性, 会在对象被attach后立即触发
        /// </summary>
        /// <returns></returns>
        [Obsolete("框架内部使用, 请勿调用.")]
        public Task Validate();
    }
    public abstract class Entity<T> : EntityBase<T>, IEntity, IModifiedOn, IHasId where T : IEntityBase
    {
        public DateTimeOffset ModifiedOn { get; set; }

        public void AddDomainEvent(params INotification[] events)
        {
            foreach (var @event in events)
            {
                (this.DbContext as GeexDbContext)?.DomainEvents.Enqueue(@event);
            }
        }

        /// <inheritdoc />
        protected IUnitOfWork Uow => base.DbContext as IUnitOfWork;

        /// <summary>校验对象合法性, 会在对象被Attach后立即触发</summary>
        /// <param name="sp">依赖注入器, 等价于this.ServiceProvider</param>
        /// <returns>A collection that holds failed-validation information.</returns>
        public virtual Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return Task.FromResult(ValidationResult.Success);
        }


        /// <summary>
        /// 用于校验对象的合法性, 会在对象被attach后立即触发
        /// </summary>
        /// <returns></returns>
        [Obsolete("框架内部使用, 请勿调用.")]
        public virtual async Task Validate()
        {
            var validationResult = (await this.Validate(this.ServiceProvider));
            if (validationResult != ValidationResult.Success)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, null, $"{validationResult.ErrorMessage}{Environment.NewLine}{validationResult.MemberNames.JoinAsString(",")}");
            }
        }
    }
}
