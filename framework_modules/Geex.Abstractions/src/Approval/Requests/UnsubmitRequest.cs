using MediatR;

namespace Geex.Abstractions.Approval
{
    public record UnSubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;
}
