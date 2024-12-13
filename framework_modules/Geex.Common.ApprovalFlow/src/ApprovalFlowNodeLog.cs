using System;
using System.Linq;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Identity.Core.Aggregates.Users;

using Microsoft.AspNetCore.Identity;

namespace Geex.Common.ApprovalFlows
{
    public class ApprovalFlowNodeLog : Entity<ApprovalFlowNodeLog>
    {
        [Obsolete("仅供EF内部使用", true)]
        public ApprovalFlowNodeLog()
        {

        }

        public ApprovalFlowNodeLog(ApprovalFlowNodeLogType auditType, string userId, string message = "", IUnitOfWork uow = default)
        {
            this.LogType = auditType;
            this.FromUserId = userId;
            var user = uow.Query<IUser>().GetById(userId);
            this.Message = this.LogType switch
            {
                ApprovalFlowNodeLogType.Withdraw => $"【{user.Nickname}】撤回了流程节点",
                ApprovalFlowNodeLogType.View => $"{user.Nickname} 查阅了流程节点",
                ApprovalFlowNodeLogType.Approve => $"通过了流程节点, 提交意见: {message}",
                ApprovalFlowNodeLogType.Reject => $"流程节点被退回: {message}",
                ApprovalFlowNodeLogType.Chat or ApprovalFlowNodeLogType.ConsultChat => message,
                _ => throw new ArgumentOutOfRangeException(nameof(auditType), auditType, null)
            };
            uow?.Attach(this);
        }
        public ApprovalFlowNodeLog(ApprovalFlowNodeLogType auditType, string fromUserId, string toUserId, string message = null, IUnitOfWork uow = default)
        {
            this.LogType = auditType;
            this.FromUserId = fromUserId;
            this.ToUserId = toUserId;
            var fromUser = uow.Query<IUser>().GetById(fromUserId);
            var toUser = uow.Query<IUser>().GetById(toUserId);

            this.Message = this.LogType switch
            {
                ApprovalFlowNodeLogType.Consult => $"向 {toUser.Nickname} 发起了征询: {message}",
                ApprovalFlowNodeLogType.CarbonCopy => $"向 {toUser.Nickname} 转发了当前审批节点",
                ApprovalFlowNodeLogType.Transfer => $"向 {toUser.Nickname} 转移了审核权限",
                _ => throw new ArgumentOutOfRangeException(nameof(auditType), auditType, null)
            };
            uow?.Attach(this);
        }

        //public ApprovalFlowNodeLog(ApprovalFlowNodeLogType logType, string fromUserId, Guid fileId, string fileName)
        //{
        //    this.LogType = logType;
        //    this.FromUserId = fromUserId;
        //    var user = IocManager.Instance.Resolve<UserManager>().FindByIdAsync(fromUserId.ToString()).Result;
        //    this.Message = $"【{user.GetDisplayName()}】撤回了流程节点";
        //}

        public string Message { get; set; }
        public ApprovalFlowNodeLogType LogType { get; set; }
        public virtual User From { get; set; }
        public string FromUserId { get; set; }
        public virtual User To { get; set; }
        public string? ToUserId { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.Now;
        public virtual ApprovalFlowNode ApprovalFlowNode { get; set; }
        public string ApprovalFlowNodeId { get; set; }
    }

    public enum ApprovalFlowNodeLogType
    {
        View,
        Consult,
        Approve,
        Transfer,
        Chat,
        Reject,
        Edit,
        ConsultChat,
        CarbonCopy,
        Withdraw,
        AttachFile
    }
}
