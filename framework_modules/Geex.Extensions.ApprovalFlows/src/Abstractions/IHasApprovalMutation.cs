using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;

namespace Geex.Extensions.ApprovalFlows;

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

    async Task<bool> Submit(string[] ids, string? remark, [Service] IUnitOfWork uow = default)
    {
        await MutateApproveEntities(uow, ids, entity => entity.Submit<T>(remark));
        return true;
    }

    async Task<bool> Approve(string[] ids, string? remark, [Service] IUnitOfWork uow = default)
    {
        await MutateApproveEntities(uow, ids, entity => entity.Approve<T>(remark));
        return true;
    }
    async Task<bool> UnSubmit(string[] ids, string? remark, [Service] IUnitOfWork uow = default)
    {
        await MutateApproveEntities(uow, ids, entity => entity.UnSubmit<T>(remark));
        return true;
    }

    async Task<bool> UnApprove(string[] ids, string? remark, [Service] IUnitOfWork uow = default)
    {
        await MutateApproveEntities(uow, ids, entity => entity.UnApprove<T>(remark));
        return true;
    }

    private static async Task MutateApproveEntities(IUnitOfWork? uow, string[] ids, Func<T, Task> mutate)
    {
        if (uow == null) throw new ArgumentNullException(nameof(uow));
        var entities = uow.Query<T>().Where(x => ids.Contains(x.Id)).ToList();
        if (!entities.Any())
        {
            throw new BusinessException(GeexExceptionType.NotFound);
        }

        foreach (var entity in entities)
        {
            if (entity is { DbContext: null })
            {
                uow.Attach(entity);
            }

            await mutate(entity);
        }
    }
}
