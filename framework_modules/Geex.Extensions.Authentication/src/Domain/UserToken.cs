using System;

using Geex.Abstractions;
using Geex.Entities;
using HotChocolate.Types;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Geex.Extensions.Authentication.Domain
{
    public class UserToken : IdentityUserToken<string>
    {
        public IUser User { get; set; }
        public new LoginProviderEnum? LoginProvider
        {
            get => (LoginProviderEnum)base.LoginProvider;
            set => base.LoginProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static UserToken New(IUser user, LoginProviderEnum provider, string token)
        {
            return new UserToken()
            {
                UserId = user.Id,
                User = user,
                Name = user.Username,
                LoginProvider = provider,
                Value = token
            };
        }

        public class UserTokenGqlType : GqlConfig.Object<UserToken>
        {
            protected override void Configure(IObjectTypeDescriptor<UserToken> descriptor)
            {
                descriptor.BindFieldsImplicitly();
                descriptor.Ignore(x => x.Value);
                descriptor.Field("token").Resolve(x => x.Parent<UserToken>().Value);
                base.Configure(descriptor);
            }
        }
    }

    public record UserTokenGenerateOptions
    {
        public string? Issuer;
        public string? Audience;
        public TimeSpan? Expires;
        public SigningCredentials? SigningCredentials;

        public UserTokenGenerateOptions(string? issuer, string audience, SigningCredentials? signingCredentials, TimeSpan? expires)
        {
            this.Issuer = issuer;
            this.Audience = audience;
            this.Expires = expires;
            this.SigningCredentials = signingCredentials;
        }
    }
}
