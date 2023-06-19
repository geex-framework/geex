using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;


namespace Geex.Common.Authentication
{
    public interface ISubClaimsTransformation
    {
        Task<ClaimsPrincipal> TransformAsync(IUser user, ClaimsPrincipal claimsPrincipal);
    }
}