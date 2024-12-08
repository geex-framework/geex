using MediatR;

namespace Geex.Common.Abstraction.Approval
{
    public record UnApproveRequest<T>(string? Remark, params string[] Ids) : IRequest;
}