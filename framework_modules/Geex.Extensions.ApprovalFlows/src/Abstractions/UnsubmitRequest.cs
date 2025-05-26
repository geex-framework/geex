using MediatR;

namespace Geex.ApprovalFlows;

public record UnSubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;