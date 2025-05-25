using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.ApprovalFlows;
using Geex.Extensions.ApprovalFlows.Requests;
using MediatR;

namespace Geex.Extensions.ApprovalFlows;

public interface IApproveRequestHandler<TInterface, TEntity> :
    ICommonHandler<TInterface, TEntity>,
    IRequestHandler<SubmitRequest<TInterface>>,
    IRequestHandler<ApproveRequest<TInterface>>,
    IRequestHandler<UnSubmitRequest<TInterface>>,
    IRequestHandler<UnApproveRequest<TInterface>>
    where TInterface : IApproveEntity where TEntity : TInterface
{

    async Task IRequestHandler<SubmitRequest<TInterface>>.Handle(SubmitRequest<TInterface> request, CancellationToken cancellationToken)
    {
        var entities = Uow.Query<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
        if (!entities.Any())
        {
            throw new BusinessException(GeexExceptionType.NotFound);
        }
        foreach (var entity in entities)
        {
            if (entity is { DbContext: null })
            {
                Uow.Attach(entity);
            }
            await entity.Submit<TInterface>(request.Remark);
        }
    }

    async Task IRequestHandler<ApproveRequest<TInterface>>.Handle(ApproveRequest<TInterface> request, CancellationToken cancellationToken)
    {
        var entities = Uow.Query<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
        if (!entities.Any())
        {
            throw new BusinessException(GeexExceptionType.NotFound);
        }
        foreach (var entity in entities)
        {
            if (entity is { DbContext: null })
            {
                Uow.Attach(entity);
            }

            await entity.Approve<TInterface>(request.Remark);
        }
    }

    async Task IRequestHandler<UnSubmitRequest<TInterface>>.Handle(UnSubmitRequest<TInterface> request, CancellationToken cancellationToken)
    {
        var entities = Uow.Query<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
        if (!entities.Any())
        {
            throw new BusinessException(GeexExceptionType.NotFound);
        }
        foreach (var entity in entities)
        {
            if (entity is { DbContext: null })
            {
                Uow.Attach(entity);
            }
            await entity.UnSubmit<TInterface>(request.Remark);
        }
    }

    async Task IRequestHandler<UnApproveRequest<TInterface>>.Handle(UnApproveRequest<TInterface> request, CancellationToken cancellationToken)
    {
        var entities = Uow.Query<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
        if (!entities.Any())
        {
            throw new BusinessException(GeexExceptionType.NotFound);
        }
        foreach (var entity in entities)
        {
            if (entity is { DbContext: null })
            {
                Uow.Attach(entity);
            }

            await entity.UnApprove<TInterface>(request.Remark);
        }
    }
}