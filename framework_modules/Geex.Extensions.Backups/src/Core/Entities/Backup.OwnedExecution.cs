using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Geex.Extensions.Backups.Core.Entities;

public partial class Backup
{
    internal async Task RecoverOwnedAsync(CancellationToken token, Func<Task> ensureOwned)
    {
        await ensureOwned();
        if (Status is BackupStatus.Running or BackupStatus.Failed or BackupStatus.Cancelled)
        {
            await CompensateAsync(BackupStatus.Failed, "Previous backup execution was interrupted.", ensureOwned, token);
            await ensureOwned();
            var options = ServiceProvider.GetRequiredService<BackupsModuleOptions>();
            var root = ServiceProvider.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
            var directory = GetWorkingDirectory(options.WorkingDirectory, root, Id);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        await ensureOwned();
        await RepairResultMessageTimeAsync(Uow, this, token);
        await SaveResultMessageAsync(ServiceProvider, Id);
    }
}
