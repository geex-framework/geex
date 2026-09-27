using Geex.Extensions.Backups.Core;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Gql.Types;
using HotChocolate.Types;
using MongoDB.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Backups.Gql;

public sealed class BackupMutation(IUnitOfWork work, BackupsExecution execution) : MutationExtension<BackupMutation>
{
    protected override void Configure(IObjectTypeDescriptor<BackupMutation> descriptor)
    {
        descriptor.Field(x => x.StartBackup()).Authorize(BackupsPermission.Create);
        descriptor.Field(x => x.StartBackupTracked()).Authorize(BackupsPermission.Create);
        descriptor.Field(x => x.ExpireBackup(default!, default)).Authorize(BackupsPermission.Expire);
        base.Configure(descriptor);
    }

    public Task<bool> StartBackup() => execution.TryStartManualAsync(work.ServiceProvider.GetService<Geex.Extensions.Authentication.ICurrentUser>()?.UserId);

    public Task<string?> StartBackupTracked() => execution.TryStartManualWithIdAsync(work.ServiceProvider.GetService<Geex.Extensions.Authentication.ICurrentUser>()?.UserId);

    public async Task<bool> ExpireBackup(string id, CancellationToken cancellationToken)
    {
        await using var lease = await BackupLease.AcquireAsync(DB.DefaultDb, DateTimeOffset.UtcNow, cancellationToken, deduplicateSlot: false);
        if (lease == null)
            return false;
        using var scope = work.ServiceProvider.CreateScope();
        var currentWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = currentWork.Query<Backup>().FirstOrDefault(x => x.Id == id)
            ?? throw new InvalidOperationException("Backup not found.");
        await backup.ExpireAsync(lease.Token, lease.EnsureOwnedAsync);
        return true;
    }
}
