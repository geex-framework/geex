using MediatR;

namespace Geex.Common.Abstraction.Approbation
{
    public record UnApproveRequest<T>(string? Remark, params string[] Ids) : IRequest;
}