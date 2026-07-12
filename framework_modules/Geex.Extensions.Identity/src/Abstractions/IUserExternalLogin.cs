using System.Collections.Generic;
using Geex.Extensions.Authentication;
using Geex.Storage;

namespace Geex.Extensions.Identity
{
    public interface IUserExternalLogin : IEntity
    {
        string UserId { get; }
        LoginProviderEnum LoginProvider { get; }
        string LoginProviderId { get; }
        List<UserClaim> LoginProviderClaims { get; }
    }
}
