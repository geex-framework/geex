using System.Threading;
using System.Threading.Tasks;

namespace Geex.MultiTenant
{
    public interface IExternalTenantSyncProvider
    {
        Task<ITenant> SyncAsync(string code, ITenant localTenant, CancellationToken cancellationToken = default);
    }
}
