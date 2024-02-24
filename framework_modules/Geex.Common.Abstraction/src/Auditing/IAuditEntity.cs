using System.Threading.Tasks;

using Geex.Common.Abstraction.Auditing.Events;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using MongoDB.Entities;

namespace Geex.Common.Abstraction.Auditing
{
    public interface IAuditEntity : IEntityBase
    {
        /// <summary>
        /// 对象审批状态
        /// </summary>
        public AuditStatus AuditStatus { get; set; }
        /// <summary>
        /// 审批操作备注文本
        /// </summary>
        public string? AuditRemark { get; set; }
        /// <summary>
        /// 上报
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        async Task Submit<TEntity>(string? remark = default)
        {
            if (this.Submittable)
            {
                this.AuditStatus |= AuditStatus.Submitted;
                this.AuditRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntitySubmittedNotification<TEntity>(this));
            }
            else
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "不满足上报条件.");
            }
        }
        /// <summary>
        /// 审批
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        async Task Audit<TEntity>(string? remark = default)
        {
            if (this.AuditStatus == AuditStatus.Submitted)
            {
                this.AuditStatus |= AuditStatus.Audited;
                this.AuditRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntityAuditedNotification<TEntity>(this));
            }
            else
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "不满足审批条件.");
            }
        }
        /// <summary>
        /// 取消上报
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        async Task UnSubmit<TEntity>(string? remark = default)
        {
            if (this.AuditStatus == AuditStatus.Submitted)
            {
                this.AuditStatus ^= AuditStatus.Submitted;
                this.AuditRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntityUnsubmittedNotification<TEntity>(this));
            }
            else if (this.AuditStatus == AuditStatus.Audited)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "已审核，无法取消上报.");
            }
        }
        /// <summary>
        /// 取消审批
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="remark"></param>
        /// <returns></returns>
        async Task UnAudit<TEntity>(string? remark = default, bool backToSubmited = false)
        {
            if (this.AuditStatus == AuditStatus.Audited)
            {
                if (backToSubmited)
                {
                    this.AuditStatus = AuditStatus.Submitted;
                }
                else
                {
                    this.AuditStatus ^= AuditStatus.Audited;
                }
                this.AuditRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntityUnauditedNotification<TEntity>(this));
            }
        }
        /// <summary>
        /// 是否满足提交条件
        /// </summary>
        bool Submittable { get; }
    }
}
