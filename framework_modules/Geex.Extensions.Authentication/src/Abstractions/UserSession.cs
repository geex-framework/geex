using System;
using HotChocolate.Types;
using Microsoft.AspNetCore.Identity;

namespace Geex.Extensions.Authentication;

public class UserSession : IdentityUserToken<string>
{
    public IAuthUser User { get; set; } = default!;
    public DateTimeOffset LastUpdatedOn { get; set; }

    public new LoginProviderEnum? LoginProvider
    {
        get => (LoginProviderEnum)base.LoginProvider!;
        set => base.LoginProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static UserSession New(IAuthUser user, LoginProviderEnum provider, string token, DateTimeOffset lastUpdatedOn)
    {
        return new UserSession
        {
            UserId = user.Id,
            User = user,
            Name = user.Username,
            LoginProvider = provider,
            Value = token,
            LastUpdatedOn = lastUpdatedOn,
        };
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
