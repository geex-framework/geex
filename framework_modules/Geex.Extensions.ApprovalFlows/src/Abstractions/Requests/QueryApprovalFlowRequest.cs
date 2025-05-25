
using System;
using System.Linq;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record QueryApprovalFlowRequest : IRequest<IQueryable<ApprovalFlow>>
{
    public string? TemplateId { get; set; }
    public string? CreatorUserId { get; set; }
    public ApprovalFlowStatus? Status { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

public record QueryApprovalFlowTemplateRequest : IRequest<IQueryable<ApprovalFlowTemplate>>
{
    public string? CreatorUserId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? OrgCode { get; set; }
}