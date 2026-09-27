using Geex.Extensions.Backups.Core.Entities;
using HotChocolate.Types;

namespace Geex.Extensions.Backups.Gql;

public sealed class BackupType : ObjectType<Backup>
{
    protected override void Configure(IObjectTypeDescriptor<Backup> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.DatabaseName);
        descriptor.Field(x => x.ScheduledAt);
        descriptor.Field(x => x.StartedAt);
        descriptor.Field(x => x.FinishedAt);
        descriptor.Field(x => x.Status);
        descriptor.Field(x => x.Source);
        descriptor.Field(x => x.ErrorMessage);
        descriptor.Field(x => x.BlobObjectId);
        descriptor.Field(x => x.File);
    }
}
