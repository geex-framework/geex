using System.Threading.Tasks;

using HotChocolate;

using MediatR;

namespace Geex.Common.Abstraction.Approbation
{
    public interface IHasApproveMutation
    {
    }
    public interface IHasApproveMutation<T> : IHasApproveMutation where T : IApproveEntity
    {
        async Task<bool> Submit([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new SubmitRequest<T>(remark, ids));
            return true;
        }

        async Task<bool> Approve([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new ApproveRequest<T>(remark, ids));
            return true;
        }
        async Task<bool> UnSubmit([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new UnsubmitRequest<T>(remark, ids));
            return true;
        }

        async Task<bool> UnApprove([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new UnApproveRequest<T>(remark, ids));
            return true;
        }
    }
}
