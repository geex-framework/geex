using MediatR;

namespace Geex.Abstractions.Approval
{
    public record UnApproveRequest<T>(string? Remark, params string[] Ids) : IRequest;
}
