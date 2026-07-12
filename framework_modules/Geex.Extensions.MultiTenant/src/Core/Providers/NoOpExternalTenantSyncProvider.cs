using System.Threading;
using System.Threading.Tasks;
using Geex.MultiTenant;
using Volo.Abp.DependencyInjection;

namespace Geex.Extensions.MultiTenant.Core.Providers
{
    public class NoOpExternalTenantSyncProvider : IExternalTenantSyncProvider, ITransientDependency
    {
        public Task<ITenant> SyncAsync(string code, ITenant localTenant, CancellationToken cancellationToken = default)
            => Task.FromResult(localTenant);
    }
}
