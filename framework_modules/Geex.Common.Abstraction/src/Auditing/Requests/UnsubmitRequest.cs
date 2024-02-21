using MediatR;

namespace Geex.Common.Abstraction.Auditing
{
    public record UnsubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;
}