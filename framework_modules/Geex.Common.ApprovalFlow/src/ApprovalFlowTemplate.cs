using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Common.ApprovalFlows
{
    public class ApprovalFlowTemplate : Entity<ApprovalFlowTemplate>
    {
        [Obsolete("仅供EF内部使用", true)]
        public ApprovalFlowTemplate()
        {

        }
        public ApprovalFlowTemplate(IApprovalFlowTemplateDate data, IUnitOfWork uow = default)
        {
            this.Name = data.Name;
            this.Description = data.Description;
            this.AreaId = data.AreaId;
            this.ApprovalFlowNodeTemplates = data.ApprovalFlowNodeTemplates.Select((x, i) => new ApprovalFlowNodeTemplate(x, i)).ToImmutableList();
            this.ApprovalFlowType = data.ApprovalFlowType;
            this.CreatorUserId = uow.ServiceProvider.GetService<ICurrentUser>()?.UserId;
            uow?.Attach(this);
        }

        public string? CreatorUserId { get; set; }
        public ApprovalFlowType ApprovalFlowType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ImmutableList<ApprovalFlowNodeTemplate> ApprovalFlowNodeTemplates { get; set; } = ImmutableList<ApprovalFlowNodeTemplate>.Empty;
        public string AreaId { get; set; }
    }

    public interface IApprovalFlowTemplateDate
    {
        string Name { get; set; }
        string Description { get; set; }
        IEnumerable<IApprovalFlowNodeTemplateData> ApprovalFlowNodeTemplates { get; }
        string AreaId { get; set; }
        ApprovalFlowType ApprovalFlowType { get; set; }
    }

    public class ApprovalFlowType : Enumeration<ApprovalFlowType>
    {
        public static ApprovalFlowType ProjectSimulationSubmission = new ApprovalFlowType(nameof(ProjectSimulationSubmission), nameof(ProjectSimulationSubmission));

        public static ApprovalFlowType ProjectCreation = new ApprovalFlowType(nameof(ProjectCreation), nameof(ProjectCreation));

        public static ApprovalFlowType BatchProjectSimulationSubmission = new ApprovalFlowType(nameof(BatchProjectSimulationSubmission), nameof(BatchProjectSimulationSubmission));

        public ApprovalFlowType(string name, string value) : base(name, value)
        {
        }
    }

    public interface IApprovalFlowDate
    {
        string Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        ImmutableList<IApprovalFlowNodeData> ApprovalFlowNodes { get; }
        string OrgCode { get; set; }
        string TemplateId { get; set; }
    }

    public interface IApprovalFlowNodeData
    {
        string Id { get; set; }
        bool IsFromTemplate { get; set; }
        string AuditRole { get; set; }
        string? AuditUserId { get; set; }
        string Description { get; set; }
        string ApprovalFlowId { get; set; }
        ImmutableList<string> CarbonCopyUserIds { get; set; }
        string Name { get; set; }
        int Index { get; set; }
    }

    public interface IApprovalFlowNodeTemplateData
    {
        string Id { get; set; }
        string AuditRole { get; set; }
        string Name { get; set; }
        int Index { get; set; }
        ImmutableList<string> CarbonCopyUserIds { get; set; }
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
            this.CarbonCopyUserIds = data.CarbonCopyUserIds;
        }

        public string Id { get; set; }
        public string AuditRole { get; set; }
        public ImmutableList<string> CarbonCopyUserIds { get; set; } = ImmutableList<string>.Empty;
        public int Index { get; set; }
        public string Name { get; set; }
    }
}
