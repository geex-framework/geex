using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.AuditLogs.Requests;
using MediatX;
using Volo.Abp.DependencyInjection;

namespace Geex.Extensions.AuditLogs.Core.Handlers;

public class AuditLogHandler :
    IRequestHandler<DeleteAuditLogsRequest, long>,
    ITransientDependency
{
    public IUnitOfWork Uow { get; }

    public AuditLogHandler(IUnitOfWork uow)
    {
        Uow = uow;
    }

    public async Task<long> Handle(DeleteAuditLogsRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return 0;

        var count = await Uow.DeleteAsync<AuditLog>(x => request.Ids.Contains(x.Id), cancellationToken);
        return count;
    }
}
