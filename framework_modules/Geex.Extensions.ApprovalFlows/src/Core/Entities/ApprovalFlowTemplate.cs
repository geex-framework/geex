using System;
using System.Collections.Generic;
using System.Linq;
using Geex.Abstractions;
using Geex.Abstractions.Authentication;
using Geex.Entities;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlowTemplate : Entity<ApprovalFlowTemplate>
{
    protected ApprovalFlowTemplate()
    {
        ConfigLazyQuery(x => x.CreatorUser, x => x.Id == this.CreatorUserId, templates => user => templates.SelectList(x => x.CreatorUserId).Contains(user.Id));
    }
    public ApprovalFlowTemplate(IApprovalFlowTemplateDate data, IUnitOfWork uow = default)
        : this()
    {
        this.Name = data.Name;
        this.Description = data.Description;
        this.OrgCode = data.OrgCode;
        this.Nodes = data.ApprovalFlowNodeTemplates.Select((x, i) => new ApprovalFlowNodeTemplate(x, i)).ToList();
        this.CreatorUserId = uow.ServiceProvider.GetService<ICurrentUser>()?.UserId;
        uow?.Attach(this);
    }

    public string? CreatorUserId { get; set; }
    public Lazy<IUser> CreatorUser => this.LazyQuery(() => CreatorUser);
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ApprovalFlowNodeTemplate> Nodes { get; set; } = new List<ApprovalFlowNodeTemplate>();
    public string OrgCode { get; set; }

    public void Edit(IApprovalFlowTemplateDate request)
    {
        if (!request.Name.IsNullOrEmpty()) this.Name = request.Name;
        if (!request.Description.IsNullOrEmpty()) this.Description = request.Description;
        if (!request.OrgCode.IsNullOrEmpty()) this.OrgCode = request.OrgCode;
        if (!request.ApprovalFlowNodeTemplates.IsNullOrEmpty())
            this.Nodes = request.ApprovalFlowNodeTemplates
                .Select((x, i) => new ApprovalFlowNodeTemplate(x, i)).ToList();
    }
}

public class ApprovalFlowNodeTemplate
{
    [Obsolete("仅供EF内部使用", true)]
    public ApprovalFlowNodeTemplate()
    {

    }
    public ApprovalFlowNodeTemplate(IApprovalFlowNodeTemplateData data, int i)
    {
        this.Id = data.Id;
        this.AuditRole = data.AuditRole;
        this.Name = data.Name;
        this.Index = i;
        this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToList();
    }

    public string Id { get; set; }
    public string AuditRole { get; set; }
    public List<string> CarbonCopyUserIds { get; set; } = new List<string>();
    public int Index { get; set; }
    public string Name { get; set; }
}