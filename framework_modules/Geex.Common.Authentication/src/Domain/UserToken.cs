using System;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Microsoft.AspNetCore.Identity;

namespace Geex.Common.Authentication.Domain
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
    }

    public record UserTokenGenerateOptions
    {
        public string Issuer;
        public string Audience;
        public TimeSpan? Expires;
        public string SecretKey;

        public UserTokenGenerateOptions(string issuer, string audience, string secretKey, TimeSpan? expires)
        {
            this.Issuer = issuer;
            this.Audience = audience;
            this.Expires = expires;
            this.SecretKey = secretKey;
        }
    }
}
