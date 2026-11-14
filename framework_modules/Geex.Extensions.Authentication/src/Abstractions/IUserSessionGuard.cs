using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Authentication;

public interface IUserSessionGuard
{
    Task BeforeStartAsync(IAuthUser user, LoginProviderEnum provider, CancellationToken cancellationToken);
}
