using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.ApprovalFlows.Events;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.MultiTenant;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

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
        this.AssociatedEntityType = data.AssociatedEntityType;
        this.AssociatedEntityId = data.AssociatedEntityId;
        uow?.Attach(this);
        this.CreatorUserId = uow?.ServiceProvider.GetService<ICurrentUser>()?.UserId;
        var template = InitializeSnapshot(data);
        data.Nodes.Select((x, i) =>
        {
            x.ApprovalFlowId = this.Id;
            x.Index = i;
            if (template != null)
            {
                var definition = template.Nodes.OrderBy(n => n.Index).ElementAt(i);
                x.Name = definition.Name;
                x.Description = definition.Description;
                x.AuditRole = definition.AuditRole;
                x.AuditUserId = string.IsNullOrWhiteSpace(x.AuditUserId) ? definition.AuditUserId : x.AuditUserId;
                x.CarbonCopyUserIds = [.. definition.CarbonCopyUserIds];
                x.IsFromTemplate = true;
            }
            var node = uow.Create(x);
            if (template != null)
                node.FormFields = template.Nodes.OrderBy(n => n.Index).ElementAt(i).FormFields.Select(f => f.Copy()).ToList();
            return node;
        }).ToList();

        this.EnsureStakeholder(uow?.ServiceProvider.GetService<ICurrentUser>()?.UserId, ApprovalFlowOwnershipType.Create);
        foreach (var userId in this.Nodes.Select(x => x.AuditUserId).Where(x => !x.IsNullOrEmpty()).Distinct())
        {
            this.EnsureStakeholder(userId, ApprovalFlowOwnershipType.Participate);
        }
        foreach (var userId in this.Nodes.SelectMany(x => x.CarbonCopyUserIds).Where(x => !x.IsNullOrEmpty()).Distinct())
        {
            this.EnsureStakeholder(userId, ApprovalFlowOwnershipType.CarbonCopy);
        }
    }

    public async Task Start()
    {
        if (AssociatedEntityType != null && !string.IsNullOrWhiteSpace(AssociatedEntityId))
        {
            var entity = this.AssociatedEntity.Value ?? throw new UserFriendlyException("关联对象不存在或无权访问.");
            await entity.Submit(this.AssociatedEntityType.Type, "审批流自动触发上报.");
        }
        await this.ActiveNode.Start();

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
            var node = new ApprovalFlowNode(x, uow);
            node.ApprovalFlowId = this.Id;
            return node;
        }).ToList();
        this.EnsureStakeholder(Uow.ServiceProvider.GetService<ICurrentUser>()?.UserId, ApprovalFlowOwnershipType.Create);
        this.OrgCode = template.OrgCode;
        this.TemplateId = template.Id;
        this.TemplateName = template.Name;
        this.Categories = [.. template.Categories];
        this.FormFields = template.FormFields.Select(x => x.Copy()).ToList();
    }

    protected ApprovalFlow()
    {
        this.ConfigLazyQuery(x => x.CreatorUser, blob => blob.Id == CreatorUserId, users => blob => users.SelectList(x => x.CreatorUserId).Contains(blob.Id));
        this.ConfigLazyQuery(x => x.Nodes, node => node.ApprovalFlowId == Id, approvalFlows => node => approvalFlows.SelectList(x => x.Id).Contains(node.ApprovalFlowId));
        this.ConfigLazyQuery(x => x.Stakeholders, stakeholder => stakeholder.ApprovalFlowId == Id, approvalFlows => stakeholder => approvalFlows.SelectList(x => x.Id).Contains(stakeholder.ApprovalFlowId));
        this.ConfigLazyQuery(x => x.AssociatedEntity, approveEntity => approveEntity.Id == AssociatedEntityId, approvalFlows => approveEntity => approvalFlows.SelectList(x => x.AssociatedEntityId).Contains(approveEntity.Id),
            () =>
            {
                var type = this.AssociatedEntityType?.Type;
                if (type == null || string.IsNullOrWhiteSpace(AssociatedEntityId))
                    return Enumerable.Empty<IApproveEntity>().AsQueryable();
                var method = typeof(IRepository).GetMethod(nameof(IRepository.Query)).MakeGenericMethod(type);
                var query = method.Invoke(Uow, []) as IQueryable<IApproveEntity>;
                return query as IQueryable<IApproveEntity>;
            });
    }

    public string? TemplateId { get; set; }


    public string? Description { get; set; }

    public string Name { get; set; }

    public IQueryable<ApprovalFlowUserRef> Stakeholders => LazyQuery(() => Stakeholders);
    public IQueryable<ApprovalFlowNode> Nodes => LazyQuery(() => Nodes).OrderBy(x=>x.Index);
    public string? CreatorUserId { get; set; }
    public Lazy<User> CreatorUser => LazyQuery(() => CreatorUser);
    public ApprovalFlowStatus Status { get; set; }
    public int ActiveIndex { get; set; }

    public ApprovalFlowNode? ActiveNode
    {
        get
        {
            return this.Nodes.FirstOrDefault(x => x.Index == this.ActiveIndex);
        }
    }

    public string? OrgCode { get; set; }

    public bool CanEdit
    {
        //这里判断第一个start的log
        get { return Status == ApprovalFlowStatus.Processing && this.ActiveIndex == 0
            && this.ActiveNode?.NodeStatus.HasFlag(ApprovalFlowNodeStatus.Approved) == false; }
    }

    public ApprovalFlowUserRef? EnsureStakeholder(string? userId, ApprovalFlowOwnershipType ownershipType)
    {
        if (userId.IsNullOrEmpty())
        {
            return null;
        }

        var existing = this.Stakeholders.FirstOrDefault(x => x.UserId == userId && x.OwnershipType == ownershipType);
        if (existing != null)
        {
            return existing;
        }

        return Uow.Attach(new ApprovalFlowUserRef(this.Id, userId!, ownershipType));
    }

    public async Task Finish()
    {
        if (Status != ApprovalFlowStatus.Processing || Nodes.Any(x =>
            (x.NodeStatus & (ApprovalFlowNodeStatus.Approved | ApprovalFlowNodeStatus.Rejected | ApprovalFlowNodeStatus.Transferred)) == 0))
            throw new UserFriendlyException("全部有效节点通过后才能完成流程.");
        this.Status = ApprovalFlowStatus.Finished;
        if (this.AssociatedEntityId != default)
        {
            await this.AssociatedEntity.Value!.Approve(this.AssociatedEntityType.Type, "审批流自动触发审批通过");
        }
        this.AddDomainEvent(new ApprovalFlowFinishEvent(this, this.Id));
    }
    /// <summary>
    /// 关联的实体对象
    /// </summary>
    public Lazy<IApproveEntity?> AssociatedEntity => LazyQuery(() => AssociatedEntity);
    /// <summary>
    /// 关联的实体对象类型
    /// </summary>
    public ApprovalFlowAssociatedEntityType? AssociatedEntityType { get; set; }
    public string? AssociatedEntityId { get; set; }

    public async Task CancelAsync()
    {
        if (Status != ApprovalFlowStatus.Processing)
            throw new UserFriendlyException("仅办理中的流程可取消.");
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

    public void Edit(EditApprovalFlowRequest request)
    {
        if (this.CanEdit)
        {
            this.Name = request.Name;
            this.Description = request.Description;
            if ((request.AssociatedEntityType != null && request.AssociatedEntityType != AssociatedEntityType)
                || (request.AssociatedEntityId != null && request.AssociatedEntityId != AssociatedEntityId))
                throw new UserFriendlyException("流程创建后关联对象保持固定.");
            var nodes = this.Nodes.ToList();
            foreach (var (node, requestNode) in nodes.Join(request.Nodes, l => l.Index, r => r.Index, (l, r) => (node: l, requestNode: r)))
            {
                node.Edit(requestNode);
            }
        }
    }
}

public class ApprovalFlowAssociatedEntityType : Enumeration<ApprovalFlowAssociatedEntityType>
{
    public Type Type { get; }

    /// <inheritdoc />
    public ApprovalFlowAssociatedEntityType(string value, Type type) : base(value)
    {
        Type = type;
    }

    public static ApprovalFlowAssociatedEntityType Object { get; } = new(nameof(Object), typeof(object));
}

public enum ApprovalFlowStatus
{
    Processing = 0,
    Finished = 1,
    Canceled = -1,
    Archived = 2
}
