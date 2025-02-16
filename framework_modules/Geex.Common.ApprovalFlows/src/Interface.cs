using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geex.Common.ApprovalFlows
{
    public interface IApprovalFlowTemplateDate
    {
        string? Name { get; set; }
        string? Description { get; set; }
        List<IApprovalFlowNodeTemplateData> ApprovalFlowNodeTemplates { get; }
        string? OrgCode { get; set; }
    }

    public interface IApprovalFlowDate
    {
        string? Name { get; set; }
        string? Description { get; set; }
        List<IApprovalFlowNodeData> Nodes { get; }
        string? OrgCode { get; set; }
        string? TemplateId { get; set; }
        public AssociatedEntityType? AssociatedEntityType { get; set; }
        public string? AssociatedEntityId { get; set; }
    }

    public interface IApprovalFlowNodeData
    {
        bool? IsFromTemplate { get; set; }
        string? AuditRole { get; set; }
        string? AuditUserId { get; set; }
        string? Description { get; set; }
        string? ApprovalFlowId { get; set; }
        List<string> CarbonCopyUserIds { get; set; }
        string? Name { get; set; }
        int? Index { get; set; }
    }

    public interface IApprovalFlowNodeTemplateData
    {
        string? Id { get; set; }
        string? AuditRole { get; set; }
        string? Name { get; set; }
        int? Index { get; set; }
        List<string> CarbonCopyUserIds { get; set; }
        public AssociatedEntityType? AssociatedEntityType { get; set; }
    }
}
