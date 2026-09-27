using Geex.Extensions.BackgroundJob;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Backups.Gql;

public sealed class BackupQuery(IUnitOfWork work, BackupsModuleOptions options, BackgroundJobModuleOptions jobs)
    : QueryExtension<BackupQuery>
{
    protected override void Configure(IObjectTypeDescriptor<BackupQuery> descriptor)
    {
        descriptor.Field(x => x.Backups())
            .UseOffsetPaging<BackupType>()
            .UseFiltering<Backup>(filter =>
            {
                filter.BindFieldsExplicitly();
                filter.Field(x => x.Id);
                filter.Field(x => x.DatabaseName);
                filter.Field(x => x.Status);
                filter.Field(x => x.ScheduledAt);
                filter.Field(x => x.StartedAt);
            })
            .Authorize(BackupsPermission.Query);
        descriptor.Field(x => x.BackupsEnabled()).Authorize(BackupsPermission.Query);
        base.Configure(descriptor);
    }

    public IQueryable<Backup> Backups() => work.Query<Backup>().OrderByDescending(x => x.ScheduledAt);
    public bool BackupsEnabled() => options.Enabled && !jobs.Disabled;
}
