using System;
using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

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
        Validate(data);
        ApprovalFormValidation.Definitions(data.FormFields);
        this.Tags = data.Tags.Distinct().ToList();
        this.FormFields = data.FormFields.Select(x => x.Copy()).ToList();
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
    public string? OrgCode { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<ApprovalFlowFormField> FormFields { get; set; } = [];

    public void Edit(IApprovalFlowTemplateDate request)
    {
        Validate(request);
        ApprovalFormValidation.Definitions(request.FormFields);
        this.Tags = request.Tags.Distinct().ToList();
        this.FormFields = request.FormFields.Select(x => x.Copy()).ToList();
        this.Name = request.Name;
        this.Description = request.Description;
        this.OrgCode = request.OrgCode;
        this.Nodes = request.ApprovalFlowNodeTemplates
            .Select((x, i) => new ApprovalFlowNodeTemplate(x, i)).ToList();
    }

    private static void Validate(IApprovalFlowTemplateDate data)
    {
        if (string.IsNullOrWhiteSpace(data.Name) || data.ApprovalFlowNodeTemplates.Count == 0
            || data.ApprovalFlowNodeTemplates.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            throw new UserFriendlyException("请填写模板名称并配置有效的审批节点.");
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
        ApprovalFormValidation.Definitions(data.FormFields);
        this.FormFields = data.FormFields.Select(x => x.Copy()).ToList();
        this.AuditUserId = data.AuditUserId;
        this.Description = data.Description;
        this.Id = string.IsNullOrWhiteSpace(data.Id) ? Guid.NewGuid().ToString("N") : data.Id;
        this.AuditRole = data.AuditRole;
        this.Name = data.Name;
        this.Index = i;
        this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToList();
    }

    public string Id { get; set; }
    public string? AuditUserId { get; set; }
    public string? Description { get; set; }
    public List<ApprovalFormField> FormFields { get; set; } = [];
    public string AuditRole { get; set; }
    public List<string> CarbonCopyUserIds { get; set; } = new List<string>();
    public int Index { get; set; }
    public string Name { get; set; }
}
