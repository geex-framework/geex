using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Abstractions.Authentication;
using Geex.Entities;
using Geex.Extensions.ApprovalFlows.Events;
using Geex.Extensions.Requests.Messaging;
using Geex.Storage;
using KuanFang.Rms.MessageManagement.Messages;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlowNode : Entity<ApprovalFlowNode>
{
    protected ApprovalFlowNode()
    {
        ConfigLazyQuery(x => x.ChatLogs, nodeLog => nodeLog.ApprovalFlowNodeId == Id, nodes => nodeLog => nodes.SelectList(x => x.Id).Contains(nodeLog.ApprovalFlowNodeId));
        ConfigLazyQuery(x => x.AuditUser, user => user.Id == AuditUserId, nodes => user => nodes.SelectList(x => x.AuditUserId).Contains(user.Id));
        ConfigLazyQuery(x => x.ConsultUser, user => user.Id == ConsultUserId, nodes => user => nodes.SelectList(x => x.ConsultUserId).Contains(user.Id));
        ConfigLazyQuery(x => x.ApprovalFlow, user => user.Id == ApprovalFlowId, nodes => approvalFlow => nodes.SelectList(x => x.ApprovalFlowId).Contains(approvalFlow.Id));
    }

    public ApprovalFlowNode(ApprovalFlowNodeTemplate data, IUnitOfWork uow = default)
        : this()
    {
        this.IsFromTemplate = true;
        this.AuditRole = data.AuditRole;
        this.Name = data.Name;
        this.Index = data.Index;
        this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToList();
        uow?.Attach(this);
    }

    public ApprovalFlowNode(IApprovalFlowNodeData data, IUnitOfWork uow = default)
        : this()
    {
        if (data.AuditUserId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("工作流节点必须包含审核用户Id");
        }
        this.IsFromTemplate = data.IsFromTemplate.GetValueOrDefault();
        this.AuditRole = data.AuditRole;
        this.Name = data.Name;
        this.Description = data.Description;
        this.AuditUserId = data.AuditUserId;
        this.ApprovalFlowId = data.ApprovalFlowId;
        this.Index = data.Index.GetValueOrDefault();
        this.CarbonCopyUserIds = data.CarbonCopyUserIds?.ToList() ?? [];
        uow?.Attach(this);
    }

    public ApprovalFlowNode(ApprovalFlowNode data, IUnitOfWork uow = default)
        : this()
    {
        this.IsFromTemplate = data.IsFromTemplate;
        this.AuditRole = data.AuditRole;
        this.Name = data.Name;
        this.Description = data.Description;
        this.AuditUserId = data.AuditUserId;
        this.ApprovalFlowId = data.ApprovalFlowId;
        this.CarbonCopyUserIds = data.CarbonCopyUserIds?.ToList() ?? [];
        uow?.Attach(this);
    }

    public Lazy<ApprovalFlow> ApprovalFlow => LazyQuery(() => ApprovalFlow);

    public Lazy<IUser> AuditUser => LazyQuery(() => AuditUser);

    public IQueryable<ApprovalFlowNodeLog> ChatLogs => LazyQuery(() => ChatLogs);

    public Lazy<IUser> ConsultUser => LazyQuery(() => ConsultUser);

    private ICurrentUser CurrentUser => this.ServiceProvider.GetService<ICurrentUser>();

    public ApprovalFlowNode NextNode
    {
        get
        {
            return this.ApprovalFlow.Value.Nodes.ToDictionary(x => x.Index, x => x).TryGetValue(this.Index + 1, out var result) ? result : null;
        }
    }

    public ApprovalFlowNode PreviousNode
    {
        get
        {
            return this.ApprovalFlow.Value.Nodes.ToDictionary(x => x.Index, x => x).TryGetValue(this.Index - 1, out var result) ? result : null;
        }
    }

    public string? ApprovalComment { get; set; }
    public string ApprovalFlowId { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string? AuditRole { get; set; }
    public string? AuditUserId { get; set; }
    public List<string> CarbonCopyUserIds { get; set; } = new List<string>();
    public string? ConsultUserId { get; set; }
    public string? Description { get; set; }
    public int Index { get; set; }
    public bool IsFromTemplate { get; set; }
    public string Name { get; set; }
    public ApprovalFlowNodeStatus NodeStatus { get; set; }

    public async Task Approve(string message)
    {
        CheckIntegrity();
        CheckAuditUser();
        CheckNotConsulting();
        this.NodeStatus |= ApprovalFlowNodeStatus.Approved;
        Uow.Create(this.Id, ApprovalFlowNodeLogType.Approve, CurrentUser.UserId, "", message);
        if (this.NextNode == default)
        {
            await this.ApprovalFlow.Value.Finish();
        }
        else
        {
            await this.NextNode.Start();
        }

        this.ApprovalFlow.Value.ActiveIndex += 1;
        this.ApprovalComment = message;
        this.ApprovalTime = DateTime.Now;
        this.AddDomainEvent(new ApprovalFlowNodeApprovedEvent(this));
    }

    public async Task BulkReject(string message, string targetNodeId)
    {
        CheckNotConsulting();
        CheckIntegrity();
        CheckAuditUser();
        this.NodeStatus |= ApprovalFlowNodeStatus.Rejected;
        this.ApprovalComment = message;
        var curIndex = this.ApprovalFlow.Value.ActiveIndex;
        var targetNode = this.ApprovalFlow.Value.Nodes.First(x => x.Id == targetNodeId);
        var nodesToReject = this.ApprovalFlow.Value.Nodes.Where(x => x.Index >= targetNode.Index && x.Index <= this.ApprovalFlow.Value.ActiveIndex).OrderBy(x => x.Index).ToList();
        foreach (var node in nodesToReject)
        {
            await node.RejectAndCopyNodes(message);
        }
        this.ApprovalFlow.Value.ActiveIndex = curIndex + 1;
        await this.NextNode.Start();
        nodesToReject.Remove(this);
        this.AddDomainEvent(new ApprovalFlowNodeBulkRejectedEvent(this, nodesToReject));
    }

    public async Task CarbonCopy(string userId)
    {
        CheckIntegrity();
        CheckAuditUser();

        if (this.CarbonCopyUserIds.Contains(userId))
        {
            throw new UserFriendlyException($"该用户已被抄送, 无需转发.");
        }

        if (!this.ApprovalFlow.Value.Stakeholders.Any(x =>
                x.ApprovalFlowId == this.ApprovalFlowId && userId == x.UserId &&
                x.OwnershipType == ApprovalFlowOwnershipType.CarbonCopy))
        {
            this.ApprovalFlow.Value.Stakeholders.Add(new ApprovalFlowUserRef(this.ApprovalFlowId, userId, ApprovalFlowOwnershipType.CarbonCopy));
        }

        this.CarbonCopyUserIds = [.. this.CarbonCopyUserIds, userId];
        Uow.Create(this.Id, ApprovalFlowNodeLogType.CarbonCopy, this.AuditUserId, userId, "");
        var user = CurrentUser.User;
        var messageEntity = await this.Uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Info,
            Text = $"【工作流】: 审批人 {user} 抄送了 {this.ApprovalFlow.Value.Name} .",
            Meta = new JsonObject([new("ApprovalFlowId", this.ApprovalFlowId)]),
        });
        await this.Uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [userId]
        });
    }
    //public ImmutableDictionary<string, string> AttachFiles { get; set; } = new Dictionary<string, string>().ToImmutableDictionary();

    private void CheckAuditUser()
    {
        if (this.AuditUserId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("节点审批人尚未确定, 无法操作尚未开始的工作流节点.");
        }
        if (this.AuditUserId != CurrentUser.UserId)
        {
            throw new UserFriendlyException("您无权操作该工作流节点, 请确认后重试, 如有相关疑问请联系客服人员.");
        }
    }


    private void CheckIntegrity()
    {
        if (ApprovalFlow.Value.Nodes.Any(x => x.AuditUserId == null))
        {
            throw new UserFriendlyException("流程信息尚未补全, 请等待流程发起者完成相关配置再行操作.");
        }
    }

    private void CheckNotConsulting()
    {
        if (this.NodeStatus.HasFlag(ApprovalFlowNodeStatus.Consulting))
        {
            throw new UserFriendlyException($"节点正处于征询状态, 请等待 {this.ConsultUser.Value.Nickname} 回复.");
        }
    }

    public async Task Consult(string userId, string message)
    {
        CheckNotConsulting();
        CheckIntegrity();
        CheckAuditUser();
        if (!this.ApprovalFlow.Value.Stakeholders.Any(x =>
                x.ApprovalFlowId == this.ApprovalFlowId && userId == x.UserId &&
                x.OwnershipType == ApprovalFlowOwnershipType.Consult))
        {
            this.ApprovalFlow.Value.Stakeholders.Add(new ApprovalFlowUserRef(this.ApprovalFlowId, userId, ApprovalFlowOwnershipType.Consult));
        }

        this.ConsultUserId = userId;
        this.NodeStatus |= ApprovalFlowNodeStatus.Consulting;
        Uow.Create(this.Id, ApprovalFlowNodeLogType.Consult, this.AuditUserId, userId, message);
        var user = CurrentUser.User;
        var messageEntity = await this.Uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Warn,
            Text = $"【工作流】: {this.ApprovalFlow.Value.Name} 的审批人 {user.Nickname} 向您发起了征询.",
            Meta = new JsonObject([new("ApprovalFlowId", this.ApprovalFlowId)]),
        });
        await this.Uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [userId]
        });
    }

    //public async Task AddAttachFile(Guid fileId)
    //{
    //    var fileName = await IocManager.Instance.Resolve<IDataFileObjectManager>().GetFileNameAsync(fileId);
    //    this.AttachFiles = this.AttachFiles.Add(fileId.ToString(), fileName);
    //    this.ApprovalFlow?.ActiveNode?.ChatLogs.Add(new ApprovalFlowNodeLog(ApprovalFlowNodeLogType.AttachFile, CurrentUser.UserId, fileId, fileName));
    //}

    public void Edit(IApprovalFlowNodeData approvalFlowNodeData)
    {
        this.AuditRole = approvalFlowNodeData.AuditRole;
        this.AuditUserId = approvalFlowNodeData.AuditUserId;
        this.Description = approvalFlowNodeData.Description;
        this.Name = approvalFlowNodeData.Name;
        this.CarbonCopyUserIds = approvalFlowNodeData.CarbonCopyUserIds;
    }

    public async void MarkAsViewed()
    {
        this.NodeStatus |= ApprovalFlowNodeStatus.Viewed;
        if (this.ChatLogs.ToList().All(x => x.LogType != ApprovalFlowNodeLogType.View))
        {
            Uow.Create(this.Id, ApprovalFlowNodeLogType.View, CurrentUser.UserId, "", "");
        }
    }

    private async Task RejectAndCopyNodes(string message)
    {
        CheckIntegrity();
        //this.NodeStatus |= ApprovalFlowNodeStatus.Rejected;
        Uow.Create(this.Id, ApprovalFlowNodeLogType.Reject, CurrentUser.UserId, "", message);
        //this.ApprovalComment = message;
        var newNode = Uow.Create(this);
        this.ApprovalFlow.Value.InsertNode(this.ApprovalFlow.Value.ActiveIndex, newNode);
        this.ApprovalFlow.Value.ActiveIndex += 1;
        //this.AddDomainEvent(new ApprovalFlowNodeRejectedEvent(this));
    }

    public async Task Reply(string message)
    {
        var userId = CurrentUser.UserId;
        if (userId == this.ConsultUserId)
        {
            this.ConsultUserId = null;
            this.NodeStatus &= ~ApprovalFlowNodeStatus.Consulting;
            Uow.Create(this.Id, ApprovalFlowNodeLogType.ConsultChat, CurrentUser.UserId, "", message);
            this.AddDomainEvent(new ApprovalFlowNodeConsultRepliedEvent(this, userId));
        }
        else
        {
            Uow.Create(this.Id, ApprovalFlowNodeLogType.Chat, CurrentUser.UserId, "", message);
        }
    }

    public async Task Start()
    {
        this.NodeStatus |= ApprovalFlowNodeStatus.Started;
        this.AddDomainEvent(new ApprovalFlowNodeStartEvent(this));
    }

    public async Task Transfer(string userId)
    {
        CheckNotConsulting();
        CheckIntegrity();
        CheckAuditUser();
        var originUserId = AuditUserId;
        this.NodeStatus |= ApprovalFlowNodeStatus.Transferred;
        if (!this.ApprovalFlow.Value.Stakeholders.Any(x => x.ApprovalFlowId == this.ApprovalFlowId && userId == x.UserId && x.OwnershipType == ApprovalFlowOwnershipType.Participate))
        {
            this.ApprovalFlow.Value.Stakeholders.Add(new ApprovalFlowUserRef(this.ApprovalFlowId, userId, ApprovalFlowOwnershipType.Participate));
        }
        Uow.Create(this.Id, ApprovalFlowNodeLogType.Transfer, this.AuditUserId, userId, "");
        var newNode = Uow.Create(this);
        newNode.AuditUserId = userId;
        newNode.ApprovalComment = "";
        this.ApprovalFlow.Value.InsertNode(this.ApprovalFlow.Value.ActiveIndex, newNode);
        this.ApprovalFlow.Value.ActiveIndex += 1;
        await this.NextNode.Start();
        this.AddDomainEvent(new ApprovalFlowNodeTransferredEvent(this, originUserId, userId));
    }

    public async Task Withdraw()
    {
        if (this.NextNode?.NodeStatus.HasFlag(ApprovalFlowNodeStatus.Viewed) != false)
        {
            throw new UserFriendlyException($"下一节点已被审批人查阅, 无法撤回.");
        }
        this.NodeStatus &= ~ApprovalFlowNodeStatus.Approved;
        this.ApprovalComment = "已撤回";
        Uow.Create(this.Id, ApprovalFlowNodeLogType.Withdraw, CurrentUser.UserId, "", "");
        this.ApprovalFlow.Value.ActiveIndex -= 1;
        var messageEntity = await this.Uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Info,
            Text = $"【工作流】: 审批人 {this.AuditUser.Value.Nickname} 撤回了审批意见.",
            Meta = new JsonObject([new("ApprovalFlowId", this.ApprovalFlowId)]),
        });
        await this.Uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [.. this.CarbonCopyUserIds]
        });
    }
}


public enum ApprovalFlowOwnershipType
{
    Create = 0,
    Participate = 1,
    Consult = 2,
    CarbonCopy = 3,
    Subscribe = 4
}

[Flags]
public enum ApprovalFlowNodeStatus
{
    Created = 0,
    Started = 1,
    Viewed = 2,
    Consulting = 4,
    Approved = 8,
    Rejected = 16,
    Transferred = 32,
}