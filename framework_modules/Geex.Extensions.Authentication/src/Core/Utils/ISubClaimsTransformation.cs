using System.Security.Claims;
using System.Threading.Tasks;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public interface ISubClaimsTransformation
    {
        Task<ClaimsPrincipal> TransformAsync(IAuthUser user, ClaimsPrincipal claimsPrincipal);
    }
}
