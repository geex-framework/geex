using MediatR;

namespace Geex.Common.Abstraction.Approbation
{
    public record UnsubmitRequest<T>(string? Remark, params string[] Ids) : IRequest;
}