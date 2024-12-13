using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Identity.Core;
using Geex.Common.Identity.Core.Aggregates.Users;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Common.ApprovalFlows
{
    public partial class ApprovalFlow : Entity<ApprovalFlow>, ITenantFilteredEntity, IOrgFilteredEntity
    {
        public ApprovalFlow(IApprovalFlowDate data, IUnitOfWork uow = default)
        {
            if (!data.ApprovalFlowNodes.Any())
            {
                throw new UserFriendlyException("工作流必须包含审批节点");
            }

            this.TemplateId = data.TemplateId;
            this.OrgCode = data.OrgCode;
            this.Name = data.Name;
            this.Description = data.Description;
            this.Nodes = data.ApprovalFlowNodes.Select((x, i) => uow.Create(x)).ToImmutableList();
            this.Stakeholders.Add(new ApprovalFlowUserRef(this.Id, uow.ServiceProvider.GetService<ICurrentUser>()?.UserId, ApprovalFlowOwnershipType.Create));
            foreach (var userId in this.Nodes.Select(x => x.AuditUserId))
            {
                if (!userId.IsNullOrEmpty())
                {
                    this.Stakeholders.Add(new ApprovalFlowUserRef(this.Id, userId, ApprovalFlowOwnershipType.Participate));
                }
            }
            foreach (var userId in this.Nodes.SelectMany(x => x.CarbonCopyUserIds))
            {
                this.Stakeholders.Add(new ApprovalFlowUserRef(this.Id, userId, ApprovalFlowOwnershipType.CarbonCopy));
            }

            this.Stakeholders =
                this.Stakeholders.DistinctBy(x => new { x.OwnershipType, x.UserId, x.ApprovalFlowId }).ToImmutableList();
            uow?.Attach(this);
        }

        public ApprovalFlow(ApprovalFlowTemplate template)
        {
            this.Name = template.Name;
            this.Description = template.Description;
            this.Nodes = template.ApprovalFlowNodeTemplates.Select((x) => new ApprovalFlowNode(x)).ToList();
            this.Stakeholders.Add(new ApprovalFlowUserRef(this.Id, Uow.ServiceProvider.GetService<ICurrentUser>().UserId, ApprovalFlowOwnershipType.Create));
            this.OrgCode = template.AreaId;
            this.TemplateId = template.Id;
            this.ApprovalFlowType = template.ApprovalFlowType;
        }

        public string TemplateId { get; set; }

        public ApprovalFlowType ApprovalFlowType { get; set; }

        public dynamic Meta { get; set; } = new Object();

        public string Description { get; set; }

        public string Name { get; set; }

        public virtual ICollection<ApprovalFlowUserRef> Stakeholders { get; set; } = new List<ApprovalFlowUserRef>();
        public virtual ICollection<ApprovalFlowNode> Nodes { get; set; } = new List<ApprovalFlowNode>();
        public DateTime CreationTime { get; set; }
        public string? CreatorUserId { get; set; }
        public virtual User CreatorUser { get; set; }
        public ApprovalFlowStatus Status { get; set; }
        public int ActiveIndex { get; set; }

        public ApprovalFlowNode ActiveNode
        {
            get
            {
                return this.Nodes.FirstOrDefault(x => x.Index == this.ActiveIndex);
            }
        }

        public string OrgCode { get; set; }

        public bool CanEdit
        {
            //这里判断第一个start的log
            get { return this.ActiveIndex == 0 && !this.ActiveNode.NodeStatus.HasFlag(ApprovalFlowNodeStatus.Approved); }
        }

        public async Task Finish()
        {
            this.Status = ApprovalFlowStatus.Finished;
            this.AddDomainEvent(new ApprovalFlowFinishEvent(this, this.Id));
        }

        public async Task CancelAsync()
        {
            this.Status = ApprovalFlowStatus.Canceled;
            this.AddDomainEvent(new ApprovalFlowCanceledEvent(this, this.Id));
        }

        public ApprovalFlow InsertNode(int index, ApprovalFlowNode approvalflowNode)
        {
            foreach (var node in this.Nodes.Where(x => x.Index > index))
            {
                node.Index += 1;
            }
            approvalflowNode.Index = index + 1;
            this.Nodes.Add(approvalflowNode);
            return this;
        }

        public struct DynamicFieldMeta
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        /// <inheritdoc />
        public string? TenantCode { get; set; }
    }

    public enum ApprovalFlowStatus
    {
        Processing = 0,
        Finished = 1,
        Canceled = -1
    }
}
