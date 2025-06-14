using MediatX;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record UnSubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;