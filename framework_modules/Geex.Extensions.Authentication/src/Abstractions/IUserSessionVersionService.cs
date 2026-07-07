using System.Threading.Tasks;

namespace Geex.Extensions.Authentication;

public interface IUserSessionVersionService
{
    Task<long> GetVersionAsync(string userId);
    Task<long> BumpVersionAsync(string userId);
    Task InvalidateSessionAsync(string userId);
}
