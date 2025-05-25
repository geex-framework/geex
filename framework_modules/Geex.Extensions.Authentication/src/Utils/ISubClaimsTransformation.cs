using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Entities;


namespace Geex.Extensions.Authentication
{
    public interface ISubClaimsTransformation
    {
        Task<ClaimsPrincipal> TransformAsync(IUser user, ClaimsPrincipal claimsPrincipal);
    }
}
