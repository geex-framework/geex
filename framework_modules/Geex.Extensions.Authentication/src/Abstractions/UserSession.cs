using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using Microsoft.AspNetCore.Identity;

namespace Geex.Extensions.Authentication;

public class UserSession : IdentityUserToken<string>, IHasId
{
    private List<CachedClaimEntry> _supplementaryClaims = new();

    public IAuthUser User { get; set; } = default!;
    public DateTimeOffset LastUpdatedOn { get; set; }
    public long Version { get; private set; }
    public IReadOnlyList<CachedClaimEntry> SupplementaryClaims => _supplementaryClaims;

    string IHasId.Id => UserId!;

    public new LoginProviderEnum? LoginProvider
    {
        get => (LoginProviderEnum)base.LoginProvider!;
        set => base.LoginProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static UserSession New(
        IAuthUser user,
        LoginProviderEnum provider,
        string token,
        DateTimeOffset lastUpdatedOn,
        long version = 0,
        IEnumerable<CachedClaimEntry>? supplementaryClaims = null)
    {
        var session = new UserSession
        {
            UserId = user.Id,
            User = user,
            Name = user.Username,
            LoginProvider = provider,
            Value = token,
            LastUpdatedOn = lastUpdatedOn,
            Version = version,
        };
        session.ReplaceSupplementaryClaims(supplementaryClaims);
        return session;
    }

    internal void Bump()
    {
        Version++;
        LastUpdatedOn = DateTimeOffset.UtcNow;
        _supplementaryClaims.Clear();
    }

    internal void SetVersion(long version) => Version = version;

    internal void ReplaceSupplementaryClaims(IEnumerable<CachedClaimEntry>? claims)
    {
        _supplementaryClaims.Clear();
        if (claims != null)
        {
            _supplementaryClaims.AddRange(claims);
        }
    }

    public class UserSessionGqlType : GqlConfig.Object<UserSession>
    {
        protected override void Configure(IObjectTypeDescriptor<UserSession> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Ignore(x => x.Value);
            descriptor.Field("token").Resolve(x => x.Parent<UserSession>().Value);
            base.Configure(descriptor);
        }
    }
}
