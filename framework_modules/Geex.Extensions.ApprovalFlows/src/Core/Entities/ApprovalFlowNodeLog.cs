using System;
using System.Linq;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Storage;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlowNodeLog : Entity<ApprovalFlowNodeLog>
{
    public ApprovalFlowNodeLog()
    {
        ConfigLazyQuery(x => x.ApprovalFlowNode, user => user.Id == ApprovalFlowNodeId, nodes => approvalFlowNode => nodes.SelectList(x => x.ApprovalFlowNodeId).Contains(approvalFlowNode.Id));
        ConfigLazyQuery(x => x.From, user => user.Id == FromUserId, logs => user => logs.SelectList(x => x.FromUserId).Contains(user.Id));
        ConfigLazyQuery(x => x.To, user => user.Id == ToUserId, logs => user => logs.SelectList(x => x.ToUserId).Contains(user.Id));
    }

    public ApprovalFlowNodeLog(string approvalFlowNodeId, ApprovalFlowNodeLogType auditType, string? fromUserId, string? toUserId, string? message, IUnitOfWork uow = default)
        : this()
    {
        this.LogType = auditType;
        this.FromUserId = fromUserId;
        this.ToUserId = toUserId;
        this.ApprovalFlowNodeId = approvalFlowNodeId;
        uow?.Attach(this);
        var fromUser = uow.Query<IUser>().GetById(fromUserId);
        var toUser = uow.Query<IUser>().GetById(toUserId);
        this.Message = this.LogType switch
        {
            ApprovalFlowNodeLogType.Consult => $"向 {toUser.Nickname} 发起了征询: {message}",
            ApprovalFlowNodeLogType.CarbonCopy => $"向 {toUser.Nickname} 转发了当前审批节点",
            ApprovalFlowNodeLogType.Transfer => $"向 {toUser.Nickname} 转移了审核权限",
            ApprovalFlowNodeLogType.Withdraw => $"【{fromUser.Nickname}】撤回了流程节点",
            ApprovalFlowNodeLogType.View => $"{fromUser.Nickname} 查阅了流程节点",
            ApprovalFlowNodeLogType.Approve => $"通过了流程节点, 提交意见: {message}",
            ApprovalFlowNodeLogType.Reject => $"流程节点被退回: {message}",
            ApprovalFlowNodeLogType.Chat => message,
            ApprovalFlowNodeLogType.ConsultChat => message,
            _ => throw new ArgumentOutOfRangeException(nameof(auditType), auditType, null)
        };
    }

    //public ApprovalFlowNodeLog(ApprovalFlowNodeLogType logType, string fromUserId, Guid fileId, string fileName)
    //{
    //    this.LogType = logType;
    //    this.FromUserId = fromUserId;
    //    var user = IocManager.Instance.Resolve<UserManager>().FindByIdAsync(fromUserId.ToString()).Result;
    //    this.Message = $"【{fromUser.Nickname}】撤回了流程节点";
    //}

    public string Message { get; set; }
    public ApprovalFlowNodeLogType LogType { get; set; }
    public Lazy<User> From => LazyQuery(() => From);
    public string FromUserId { get; set; }
    public Lazy<User> To => LazyQuery(() => To);
    public string? ToUserId { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.Now;
    public virtual Lazy<ApprovalFlowNode> ApprovalFlowNode => LazyQuery(() => ApprovalFlowNode);
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
