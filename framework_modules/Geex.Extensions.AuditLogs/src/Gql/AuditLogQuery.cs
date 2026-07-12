using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace Geex.Extensions.AuditLogs.Gql;

public sealed class AuditLogQuery : QueryExtension<AuditLogQuery>
{
    private readonly IUnitOfWork _uow;

    public AuditLogQuery(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<AuditLogQuery> descriptor)
    {
        descriptor.Field(x => x.AuditLogs())
            .UseOffsetPaging<ObjectType<AuditLog>>()
            .UseFiltering<AuditLog>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Id);
                x.Field(y => y.OperationName);
                x.Field(y => y.OperatorId);
                x.Field(y => y.IsSuccess);
                x.Field(y => y.TenantCode);
                x.Field(y => y.CreatedOn);
            })
            .UseSorting<AuditLog>()
            .Authorize(AuditLogsPermission.Query);
        base.Configure(descriptor);
    }

    public Task<IQueryable<AuditLog>> AuditLogs()
        => Task.FromResult(_uow.Query<AuditLog>().AsQueryable());
}
