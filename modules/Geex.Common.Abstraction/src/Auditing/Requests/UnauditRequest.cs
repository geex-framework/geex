using MediatR;

namespace Geex.Common.Abstraction.Auditing
{
    public record UnauditRequest<T>(string? Remark, params string[] Ids) : IRequest<Unit>;
}