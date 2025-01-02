using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Identity.Core.Aggregates.Users;
using Geex.Common.Requests.Messaging;

using KuanFang.Rms.MessageManagement.Messages;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;

namespace Geex.Common.ApprovalFlows
{
    public partial class ApprovalFlowNode : Entity<ApprovalFlowNode>
    {
        protected ApprovalFlowNode()
        {
            ConfigLazyQuery(x => x.ChatLogs, nodeLog => nodeLog.ApprovalFlowNodeId == Id, nodes => nodeLog => nodes.SelectList(x => x.Id).Contains(nodeLog.ApprovalFlowNodeId));
        }

        public ApprovalFlowNode(ApprovalFlowNodeTemplate data, IUnitOfWork uow = default)
        : this()
        {
            this.IsFromTemplate = true;
            this.AuditRole = data.AuditRole;
            this.Name = data.Name;
            this.Index = data.Index;
            this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToImmutableList();
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
            this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToImmutableList();
        }

        private ApprovalFlowNode(ApprovalFlowNode data)
        : this()
        {
            this.IsFromTemplate = data.IsFromTemplate;
            this.AuditRole = data.AuditRole;
            this.Name = data.Name;
            this.Description = data.Description;
            this.AuditUserId = data.AuditUserId;
            this.ApprovalFlowId = data.ApprovalFlowId;
            this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToImmutableList();
        }

        private ICurrentUser CurrentUser => this.ServiceProvider.GetService<ICurrentUser>();
        public string? AuditRole { get; set; }

        public virtual IQueryable<ApprovalFlowNodeLog> ChatLogs => LazyQuery(() => ChatLogs);
        public bool IsFromTemplate { get; set; }
        public virtual ApprovalFlow ApprovalFlow { get; set; }
        public string ApprovalFlowId { get; set; }
        public ApprovalFlowNode PreviousNode
        {
            get
            {
                return this.ApprovalFlow.Nodes.ToDictionary(x => x.Index, x => x).TryGetValue(this.Index - 1, out var result) ? result : null;
            }
        }

        public ApprovalFlowNode NextNode
        {
            get
            {
                return this.ApprovalFlow.Nodes.ToDictionary(x => x.Index, x => x).TryGetValue(this.Index + 1, out var result) ? result : null;
            }
        }

        public virtual User AuditUser { get; set; }
        public string? AuditUserId { get; set; }
        public ApprovalFlowNodeStatus NodeStatus { get; set; }
        public string Name { get; set; }
        public DateTime? ApprovalTime { get; set; }
        public string Description { get; set; }
        public ImmutableList<string> CarbonCopyUserIds { get; set; } = ImmutableList<string>.Empty;

        public async Task Approve(string message)
        {
            CheckIntegrity();
            CheckAuditUser();
            CheckNotConsultring();
            this.NodeStatus |= ApprovalFlowNodeStatus.Approved;
            Uow.Create(ApprovalFlowNodeLogType.Approve, CurrentUser.UserId, message);
            if (this.NextNode == default)
            {
                await this.ApprovalFlow.Finish();
            }
            else
            {
                await this.NextNode.Start();
            }

            this.ApprovalFlow.ActiveIndex += 1;
            this.ApprovalComment = message;
            this.ApprovalTime = DateTime.Now;
            this.AddDomainEvent(new ApprovalFlowNodeApprovedEvent(this));
        }

        private void CheckNotConsultring()
        {
            if (this.NodeStatus.HasFlag(ApprovalFlowNodeStatus.Consulting))
            {
                throw new UserFriendlyException($"节点正处于征询状态, 请等待 {this.ConsultUser.Nickname} 回复.");
            }
        }

        public virtual User ConsultUser { get; set; }
        public string? ConsultUserId { get; set; }


        private void CheckIntegrity()
        {
            if (ApprovalFlow.Nodes.Any(x => x.AuditUserId == null))
            {
                throw new UserFriendlyException("流程信息尚未补全, 请等待流程发起者完成相关配置再行操作.");
            }
        }

        public async Task Start()
        {
            this.NodeStatus |= ApprovalFlowNodeStatus.Started;
            this.AddDomainEvent(new ApprovalFlowNodeStartEvent(this));
        }

        public string ApprovalComment { get; set; }
        public int Index { get; set; }
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

        public async Task BulkReject(string message, string targetNodeId)
        {
            CheckNotConsultring();
            CheckIntegrity();
            CheckAuditUser();
            this.NodeStatus |= ApprovalFlowNodeStatus.Rejected;
            this.ApprovalComment = message;
            var curIndex = this.ApprovalFlow.ActiveIndex;
            var targetNode = this.ApprovalFlow.Nodes.First(x => x.Id == targetNodeId);
            var nodesToReject = this.ApprovalFlow.Nodes.Where(x => x.Index >= targetNode.Index && x.Index <= this.ApprovalFlow.ActiveIndex).OrderBy(x => x.Index).ToList();
            foreach (var node in nodesToReject)
            {
                await node.RejectAndCopyNodes(message);
            }
            this.ApprovalFlow.ActiveIndex = curIndex + 1;
            await this.NextNode.Start();
            nodesToReject.Remove(this);
            this.AddDomainEvent(new ApprovalFlowNodeBulkRejectedEvent(this, nodesToReject));
        }

        private async Task RejectAndCopyNodes(string message)
        {
            CheckIntegrity();
            //this.NodeStatus |= ApprovalFlowNodeStatus.Rejected;
            Uow.Create(ApprovalFlowNodeLogType.Reject, CurrentUser.UserId, message);
            //this.ApprovalComment = message;
            this.ApprovalFlow.InsertNode(this.ApprovalFlow.ActiveIndex, new ApprovalFlowNode(this));
            this.ApprovalFlow.ActiveIndex += 1;
            //this.AddDomainEvent(new ApprovalFlowNodeRejectedEvent(this));
        }

        public async Task Transfer(string userId)
        {
            CheckNotConsultring();
            CheckIntegrity();
            CheckAuditUser();
            var originUserId = AuditUserId;
            this.NodeStatus |= ApprovalFlowNodeStatus.Transferred;
            if (!this.ApprovalFlow.Stakeholders.Any(x => x.ApprovalFlowId == this.ApprovalFlowId && userId == x.UserId && x.OwnershipType == ApprovalFlowOwnershipType.Participate))
            {
                this.ApprovalFlow.Stakeholders.Add(new ApprovalFlowUserRef(this.ApprovalFlowId, userId, ApprovalFlowOwnershipType.Participate));
            }
            Uow.Create(ApprovalFlowNodeLogType.Transfer, this.AuditUserId, userId);
            this.ApprovalFlow.InsertNode(this.ApprovalFlow.ActiveIndex, new ApprovalFlowNode(this)
            {
                AuditUserId = userId,
                ApprovalComment = "",
            });
            this.ApprovalFlow.ActiveIndex += 1;
            await this.NextNode.Start();
            this.AddDomainEvent(new ApprovalFlowNodeTransferredEvent(this, originUserId, userId));
        }
        public async Task Consult(string userId, string message)
        {
            CheckNotConsultring();
            CheckIntegrity();
            CheckAuditUser();
            if (!this.ApprovalFlow.Stakeholders.Any(x =>
                x.ApprovalFlowId == this.ApprovalFlowId && userId == x.UserId &&
                x.OwnershipType == ApprovalFlowOwnershipType.Consult))
            {
                this.ApprovalFlow.Stakeholders.Add(new ApprovalFlowUserRef(this.ApprovalFlowId, userId, ApprovalFlowOwnershipType.Consult));
            }

            this.ConsultUserId = userId;
            this.NodeStatus |= ApprovalFlowNodeStatus.Consulting;
            Uow.Create(ApprovalFlowNodeLogType.Consult, this.AuditUserId, userId);
            var user = CurrentUser.User;
            var messageEntity = await this.Uow.Request(new CreateMessageRequest()
            {
                Severity = MessageSeverityType.Warn,
                Text = $"【工作流】: {this.ApprovalFlow.Name} 的审批人 {user.Nickname} 向您发起了征询.",
                Meta = new JsonObject([new("ApprovalFlowId", this.ApprovalFlowId)]),
            });
            await this.Uow.Request(new SendNotificationMessageRequest()
            {
                MessageId = messageEntity.Id,
                ToUserIds = [userId]
            });
        }

        public async void MarkAsViewed()
        {
            this.NodeStatus |= ApprovalFlowNodeStatus.Viewed;
            if (this.ChatLogs.All(x => x.LogType != ApprovalFlowNodeLogType.View))
            {
                Uow.Create(ApprovalFlowNodeLogType.View, CurrentUser.UserId);
            }
        }

        public async Task Withdraw()
        {
            if (this.NextNode?.NodeStatus.HasFlag(ApprovalFlowNodeStatus.Viewed) != false)
            {
                throw new UserFriendlyException($"下一节点已被审批人查阅, 无法撤回.");
            }
            this.NodeStatus &= ~ApprovalFlowNodeStatus.Approved;
            this.ApprovalComment = "已撤回";
            Uow.Create(ApprovalFlowNodeLogType.Withdraw, CurrentUser.UserId);
            this.ApprovalFlow.ActiveIndex -= 1;
            var messageEntity = await this.Uow.Request(new CreateMessageRequest()
            {
                Severity = MessageSeverityType.Info,
                Text = $"【工作流】: 审批人 {this.AuditUser.Nickname} 撤回了审批意见.",
                Meta = new JsonObject([new("ApprovalFlowId", this.ApprovalFlowId)]),
            });
            await this.Uow.Request(new SendNotificationMessageRequest()
            {
                MessageId = messageEntity.Id,
                ToUserIds = [.. this.CarbonCopyUserIds]
            });
        }

        public async Task CarbonCopy(string userId)
        {
            CheckIntegrity();
            CheckAuditUser();

            if (this.CarbonCopyUserIds.Contains(userId))
            {
                throw new UserFriendlyException($"该用户已被抄送, 无需转发.");
            }

            if (!this.ApprovalFlow.Stakeholders.Any(x =>
                x.ApprovalFlowId == this.ApprovalFlowId && userId == x.UserId &&
                x.OwnershipType == ApprovalFlowOwnershipType.CarbonCopy))
            {
                this.ApprovalFlow.Stakeholders.Add(new ApprovalFlowUserRef(this.ApprovalFlowId, userId, ApprovalFlowOwnershipType.CarbonCopy));
            }

            this.CarbonCopyUserIds = this.CarbonCopyUserIds.Add(userId);
            Uow.Create(ApprovalFlowNodeLogType.CarbonCopy, this.AuditUserId, userId);
            var user = CurrentUser.User;
            var messageEntity = await this.Uow.Request(new CreateMessageRequest()
            {
                Severity = MessageSeverityType.Info,
                Text = $"【工作流】: 审批人 {user} 转发了 {this.ApprovalFlow.Name} .",
                Meta = new JsonObject([new("ApprovalFlowId", this.ApprovalFlowId)]),
            });
            await this.Uow.Request(new SendNotificationMessageRequest()
            {
                MessageId = messageEntity.Id,
                ToUserIds = [userId]
            });
        }

        public async Task Reply(string message)
        {
            var userId = CurrentUser.UserId;
            if (userId == this.ConsultUserId)
            {
                this.ConsultUserId = null;
                this.NodeStatus &= ~ApprovalFlowNodeStatus.Consulting;
                Uow.Create(ApprovalFlowNodeLogType.ConsultChat, CurrentUser.UserId, message);
                this.AddDomainEvent(new ApprovalFlowNodeConsultRepliedEvent(this, userId));
            }
            else
            {
                Uow.Create(ApprovalFlowNodeLogType.Chat, CurrentUser.UserId, message);
            }
        }

        //public async Task AddAttachFile(Guid fileId)
        //{
        //    var fileName = await IocManager.Instance.Resolve<IDataFileObjectManager>().GetFileNameAsync(fileId);
        //    this.AttachFiles = this.AttachFiles.Add(fileId.ToString(), fileName);
        //    this.ApprovalFlow?.ActiveNode?.ChatLogs.Add(new ApprovalFlowNodeLog(ApprovalFlowNodeLogType.AttachFile, CurrentUser.UserId, fileId, fileName));
        //}

    }



    public class ApprovalFlowUserRef : Entity<ApprovalFlowUserRef>
    {
        public ApprovalFlowUserRef()
        {

        }

        public ApprovalFlowUserRef(string approvalflowId, string userId, ApprovalFlowOwnershipType ownershipType)
        {
            ApprovalFlowId = approvalflowId;
            UserId = userId;
            OwnershipType = ownershipType;
        }
        public string ApprovalFlowId { get; set; }
        public string UserId { get; set; }
        public ApprovalFlowOwnershipType OwnershipType { get; set; }
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
}
