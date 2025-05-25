using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record CreateApprovalFlowTemplateRequest : IRequest<ApprovalFlowTemplate>, IApprovalFlowTemplateDate
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string OrgCode { get; set; }
    List<IApprovalFlowNodeTemplateData> IApprovalFlowTemplateDate.ApprovalFlowNodeTemplates => this.ApprovalFlowNodeTemplates.Cast<IApprovalFlowNodeTemplateData>().ToList();
    public List<ApprovalFlowNodeTemplateData> ApprovalFlowNodeTemplates { get; set; }
}