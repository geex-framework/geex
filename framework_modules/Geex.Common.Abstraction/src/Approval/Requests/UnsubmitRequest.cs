using MediatR;

namespace Geex.Common.Abstraction.Approval
{
    public record UnSubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;
}
