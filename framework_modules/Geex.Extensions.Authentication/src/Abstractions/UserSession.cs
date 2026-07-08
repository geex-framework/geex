using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HotChocolate.Types;

namespace Geex.Extensions.Authentication;

public sealed record SupplementaryClaim(string Type, string Value);

public class UserSession : IHasId
{
    private List<SupplementaryClaim> _supplementaryClaims = new();

    public string UserId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public LoginProviderEnum LoginProvider { get; set; }
    public string Token { get; set; } = string.Empty;
    [JsonIgnore]
    public IAuthUser User { get; set; } = default!;
    public DateTimeOffset LastUpdatedOn { get; set; }
    [JsonInclude]
    public long Version { get; private set; }
    public IReadOnlyList<SupplementaryClaim> SupplementaryClaims => _supplementaryClaims;

    string IHasId.Id => UserId;

    public static UserSession New(
        IAuthUser user,
        LoginProviderEnum provider,
        string token,
        DateTimeOffset lastUpdatedOn,
        long version = 0,
        IEnumerable<SupplementaryClaim>? supplementaryClaims = null)
    {
        var session = new UserSession
        {
            UserId = user.Id,
            User = user,
            Name = user.Username,
            LoginProvider = provider,
            Token = token,
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

    internal void ReplaceSupplementaryClaims(IEnumerable<SupplementaryClaim>? claims)
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
            base.Configure(descriptor);
        }
    }
}
