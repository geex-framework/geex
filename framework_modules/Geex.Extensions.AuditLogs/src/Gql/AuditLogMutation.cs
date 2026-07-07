using System.Threading.Tasks;
using Geex.Extensions.AuditLogs.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.AuditLogs.Gql;

public sealed class AuditLogMutation : MutationExtension<AuditLogMutation>
{
    private readonly IUnitOfWork _uow;

    public AuditLogMutation(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<AuditLogMutation> descriptor)
    {
        descriptor.Field(x => x.DeleteAuditLogs(default)).Authorize(AuditLogsPermission.Delete);
        base.Configure(descriptor);
    }

    public async Task<long> DeleteAuditLogs(DeleteAuditLogsRequest request)
        => await _uow.Request(request);
}
