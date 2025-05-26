using MediatR;

namespace Geex.ApprovalFlows;

public record UnApproveRequest<T>(string? Remark, params string[] Ids) : IRequest;