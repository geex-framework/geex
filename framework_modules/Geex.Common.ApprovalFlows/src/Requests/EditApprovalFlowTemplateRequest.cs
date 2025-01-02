
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Geex.Common.ApprovalFlows;
using MediatR;

public record EditApprovalFlowTemplateRequest : IRequest<ApprovalFlowTemplate>, IApprovalFlowTemplateDate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <inheritdoc />
    public string? OrgCode { get; set; }
    public ApprovalFlowType ApprovalFlowType { get; set; }
    List<IApprovalFlowNodeTemplateData> IApprovalFlowTemplateDate.ApprovalFlowNodeTemplates => ApprovalFlowNodeTemplates.Cast<IApprovalFlowNodeTemplateData>().ToList();
    public List<ApprovalFlowNodeTemplateData> ApprovalFlowNodeTemplates { get; set; }
}
