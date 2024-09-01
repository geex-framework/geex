using System.Threading.Tasks;

using HotChocolate;

using MediatR;

namespace Geex.Common.Abstraction.Approbation
{
    public interface IHasApproveMutation
    {
        Task<bool> Submit(string[] ids, string? remark);
        Task<bool> Approve(string[] ids, string? remark);
        Task<bool> UnSubmit(string[] ids, string? remark);
        Task<bool> UnApprove(string[] ids, string? remark);
    }
    public interface IHasApproveMutation<T> : IHasApproveMutation where T : IApproveEntity
    {
        Task<bool> IHasApproveMutation.Submit(string[] ids, string? remark) => this.Submit(ids, remark);
        Task<bool> IHasApproveMutation.Approve(string[] ids, string? remark) => this.Approve(ids, remark);
        Task<bool> IHasApproveMutation.UnSubmit(string[] ids, string? remark) => this.UnSubmit(ids, remark);
        Task<bool> IHasApproveMutation.UnApprove(string[] ids, string? remark) => this.UnApprove(ids, remark);

        async Task<bool> Submit(string[] ids, string? remark, [Service] IMediator mediator = default)
        {
            await mediator.Send(new SubmitRequest<T>(remark, ids));
            return true;
        }

        async Task<bool> Approve(string[] ids, string? remark, [Service] IMediator mediator = default)
        {
            await mediator.Send(new ApproveRequest<T>(remark, ids));
            return true;
        }
        async Task<bool> UnSubmit(string[] ids, string? remark, [Service] IMediator mediator = default)
        {
            await mediator.Send(new UnSubmitRequest<T>(remark, ids));
            return true;
        }

        async Task<bool> UnApprove(string[] ids, string? remark, [Service] IMediator mediator = default)
        {
            await mediator.Send(new UnApproveRequest<T>(remark, ids));
            return true;
        }
    }
}
