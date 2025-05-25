using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatR;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record EditApprovalFlowTemplateRequest : IRequest<ApprovalFlowTemplate>, IApprovalFlowTemplateDate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <inheritdoc />
    public string? OrgCode { get; set; }
    List<IApprovalFlowNodeTemplateData> IApprovalFlowTemplateDate.ApprovalFlowNodeTemplates => ApprovalFlowNodeTemplates.Cast<IApprovalFlowNodeTemplateData>().ToList();
    public List<ApprovalFlowNodeTemplateData> ApprovalFlowNodeTemplates { get; set; }
}