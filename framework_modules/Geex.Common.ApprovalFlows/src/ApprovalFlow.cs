using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Approval;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using Geex.Common.Identity.Core;
using Geex.Common.Identity.Core.Aggregates.Users;

using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;

namespace Geex.Common.ApprovalFlows
{
    public partial class ApprovalFlow : Entity<ApprovalFlow>, ITenantFilteredEntity, IOrgFilteredEntity
    {
        public ApprovalFlow(IApprovalFlowDate data, IUnitOfWork uow = default)
        : this()
        {
            if (!data.Nodes.Any())
            {
                throw new UserFriendlyException("工作流必须包含审批节点");
            }
            this.TemplateId = data.TemplateId;
            this.OrgCode = data.OrgCode;
            this.Name = data.Name;
            this.Description = data.Description;
            uow?.Attach(this);
            this.CreatorUserId = uow?.ServiceProvider.GetService<ICurrentUser>()?.UserId;

            data.Nodes.Select((x, i) =>
            {
                x.ApprovalFlowId = this.Id;
                return uow.Create(x);
            }).ToList();
            List<ApprovalFlowUserRef> stakeholders = [new ApprovalFlowUserRef(this.Id, uow.ServiceProvider.GetService<ICurrentUser>()?.UserId, ApprovalFlowOwnershipType.Create)];
            foreach (var userId in this.Nodes.Select(x => x.AuditUserId))
            {
                if (!userId.IsNullOrEmpty())
                {
                    stakeholders.Add(new ApprovalFlowUserRef(this.Id, userId, ApprovalFlowOwnershipType.Participate));
                }
            }
            foreach (var userId in this.Nodes.SelectMany(x => x.CarbonCopyUserIds))
            {
                stakeholders.Add(new ApprovalFlowUserRef(this.Id, userId, ApprovalFlowOwnershipType.CarbonCopy));
            }

            this.Stakeholders = stakeholders.DistinctBy(x => new { x.OwnershipType, x.UserId, x.ApprovalFlowId }).ToList();
        }

        public ApprovalFlow(ApprovalFlowTemplate template, IUnitOfWork uow = default)
        : this()
        {
            this.Name = template.Name;
            this.Description = template.Description;
            uow?.Attach(this);
            this.CreatorUserId = uow?.ServiceProvider.GetService<ICurrentUser>()?.UserId;
            template.Nodes.Select((x) =>
            {
                var node = new ApprovalFlowNode(x);
                node.ApprovalFlowId = this.Id;
                return node;
            }).ToList();
            this.Stakeholders = [.. Stakeholders, new ApprovalFlowUserRef(this.Id, Uow.ServiceProvider.GetService<ICurrentUser>().UserId, ApprovalFlowOwnershipType.Create)];
            this.OrgCode = template.OrgCode;
            this.TemplateId = template.Id;
        }

        protected ApprovalFlow()
        {
            this.ConfigLazyQuery(x => x.CreatorUser, blob => blob.Id == CreatorUserId, users => blob => users.SelectList(x => x.CreatorUserId).Contains(blob.Id));
            this.ConfigLazyQuery(x => x.Nodes, node => node.ApprovalFlowId == Id, approvalFlows => node => approvalFlows.SelectList(x => x.Id).Contains(node.ApprovalFlowId));
            this.ConfigLazyQuery(x => x.AssociatedEntity, approveEntity => approveEntity.Id == AssociatedEntityId, approvalFlows => approveEntity => approvalFlows.SelectList(x => x.AssociatedEntityId).Contains(approveEntity.Id),
                () =>
                {
                    var type = this.AssociatedEntityType.Type;
                    var parameter = Expression.Parameter(typeof(IUnitOfWork), "uow");
                    var genericMethod = typeof(IUnitOfWork).GetMethod("Query").MakeGenericMethod(type);
                    var lambda = Expression.Lambda(
                        Expression.Call(parameter, genericMethod),
                        parameter
                    );
                    var compiled = (Func<IUnitOfWork, object>)lambda.Compile();
                    var result = compiled(Uow);
                    return result as IQueryable<IApproveEntity>;
                });
        }

        public string? TemplateId { get; set; }


        public string? Description { get; set; }

        public string Name { get; set; }

        public List<ApprovalFlowUserRef> Stakeholders { get; set; } = new List<ApprovalFlowUserRef>();
        public IQueryable<ApprovalFlowNode> Nodes => LazyQuery(() => Nodes);
        public string? CreatorUserId { get; set; }
        public Lazy<User> CreatorUser => LazyQuery(() => CreatorUser);
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
            if (this.AssociatedEntityId != default)
            {
                await this.AssociatedEntity.Value.Approve(this.GetType(), "审批流自动审批通过");
            }
            this.AddDomainEvent(new ApprovalFlowFinishEvent(this, this.Id));
        }
        /// <summary>
        /// 关联的实体对象
        /// </summary>
        public Lazy<IApproveEntity> AssociatedEntity => LazyQuery(() => AssociatedEntity);
        /// <summary>
        /// 关联的实体对象类型
        /// </summary>
        public AssociatedEntityType AssociatedEntityType { get; set; }
        public string? AssociatedEntityId { get; set; }

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
            approvalflowNode.ApprovalFlowId = this.Id;
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

    public class AssociatedEntityType : Enumeration<AssociatedEntityType>
    {
        public Type Type { get; }

        /// <inheritdoc />
        public AssociatedEntityType(string value, Type type) : base(value)
        {
            Type = type;
        }

        public static AssociatedEntityType Object { get; } = new(nameof(Object), typeof(object));
    }

    public enum ApprovalFlowStatus
    {
        Processing = 0,
        Finished = 1,
        Canceled = -1
    }
}
