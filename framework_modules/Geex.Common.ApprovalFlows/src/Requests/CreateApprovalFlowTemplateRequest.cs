
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Geex.Common.ApprovalFlows;
using Geex.Common.ApprovalFlows.Requests;

using MediatX;

public record CreateApprovalFlowTemplateRequest : IRequest<ApprovalFlowTemplate>, IApprovalFlowTemplateDate
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string OrgCode { get; set; }
    List<IApprovalFlowNodeTemplateData> IApprovalFlowTemplateDate.ApprovalFlowNodeTemplates => this.ApprovalFlowNodeTemplates.Cast<IApprovalFlowNodeTemplateData>().ToList();
    public List<ApprovalFlowNodeTemplateData> ApprovalFlowNodeTemplates { get; set; }
}
