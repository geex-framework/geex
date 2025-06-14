using MediatX;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record UnApproveRequest<T>(string? Remark, params string[] Ids) : IRequest;