using System.Threading.Tasks;
using Geex.Common.Abstraction.Approbation.Events;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using MongoDB.Entities;

namespace Geex.Common.Abstraction.Approbation
{
    public interface IApproveEntity : IEntityBase
    {
        /// <summary>
        /// 对象审批状态
        /// </summary>
        public ApproveStatus ApproveStatus { get; set; }
        /// <summary>
        /// 审批操作备注文本
        /// </summary>
        public string? ApproveRemark { get; set; }
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
                this.ApproveStatus |= ApproveStatus.Submitted;
                this.ApproveRemark = remark;
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
        async Task Approve<TEntity>(string? remark = default)
        {
            if (this.ApproveStatus == ApproveStatus.Submitted)
            {
                this.ApproveStatus |= ApproveStatus.Approved;
                this.ApproveRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntityApprovedNotification<TEntity>(this));
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
            if (this.ApproveStatus == ApproveStatus.Submitted)
            {
                this.ApproveStatus ^= ApproveStatus.Submitted;
                this.ApproveRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntityUnSubmittedNotification<TEntity>(this));
            }
            else if (this.ApproveStatus == ApproveStatus.Approved)
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
        async Task UnApprove<TEntity>(string? remark = default, bool backToSubmited = false)
        {
            if (this.ApproveStatus == ApproveStatus.Approved)
            {
                if (backToSubmited)
                {
                    this.ApproveStatus = ApproveStatus.Submitted;
                }
                else
                {
                    this.ApproveStatus ^= ApproveStatus.Approved;
                }
                this.ApproveRemark = remark;
                (this as IEntity)?.AddDomainEvent(new EntityUnApprovedNotification<TEntity>(this));
            }
        }
        /// <summary>
        /// 是否满足提交条件
        /// </summary>
        bool Submittable { get; }
    }
}
