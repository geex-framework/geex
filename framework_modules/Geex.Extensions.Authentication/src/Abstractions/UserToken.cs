using System;
using HotChocolate.Types;
using Microsoft.AspNetCore.Identity;

namespace Geex.Extensions.Authentication
{
    public class UserToken : IdentityUserToken<string>
    {
        public IAuthUser User { get; set; }
        public new LoginProviderEnum? LoginProvider
        {
            get => (LoginProviderEnum)base.LoginProvider;
            set => base.LoginProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static UserToken New(IAuthUser user, LoginProviderEnum provider, string token)
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
}
