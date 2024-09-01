using MediatR;

namespace Geex.Common.Abstraction.Approbation
{
    public record UnSubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;
}
