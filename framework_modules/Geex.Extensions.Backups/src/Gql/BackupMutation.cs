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
        descriptor.Field(x => x.StartBackup(default)).Authorize(BackupsPermission.Create);
        descriptor.Field(x => x.StartBackupTracked(default)).Authorize(BackupsPermission.Create);
        descriptor.Field(x => x.ExpireBackup(default!, default)).Authorize(BackupsPermission.Expire);
        base.Configure(descriptor);
    }

    public Task<bool> StartBackup(bool force = false) => execution.TryStartManualAsync(work.ServiceProvider.GetService<Geex.Extensions.Authentication.ICurrentUser>()?.UserId, force);

    public Task<string?> StartBackupTracked(bool force = false) => execution.TryStartManualWithIdAsync(work.ServiceProvider.GetService<Geex.Extensions.Authentication.ICurrentUser>()?.UserId, force);

    public async Task<bool> ExpireBackup(string id, CancellationToken cancellationToken)
    {
        await using var execution = await BackupLock.AcquireAsync(work.ServiceProvider, cancellationToken);
        if (execution == null)
            return false;
        using var scope = work.ServiceProvider.CreateScope();
        var currentWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = currentWork.Query<Backup>().FirstOrDefault(x => x.Id == id)
            ?? throw new InvalidOperationException("Backup not found.");
        await backup.ExpireAsync(execution.Token);
        return true;
    }
}
