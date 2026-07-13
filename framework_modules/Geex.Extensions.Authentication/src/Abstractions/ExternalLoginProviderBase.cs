using System.Threading.Tasks;

namespace Geex.Extensions.Authentication
{
    public abstract class ExternalLoginProviderBase : IExternalLoginProvider
    {
        private readonly IExternalAccountLinker _externalAccountLinker;

        protected ExternalLoginProviderBase(IExternalAccountLinker externalAccountLinker)
        {
            _externalAccountLinker = externalAccountLinker;
        }

        public abstract LoginProviderEnum Provider { get; }

        public abstract Task<ExternalLoginIdentity> ResolveIdentity(string code);

        public virtual async Task<IAuthUser> ExternalLogin(string code)
        {
            var identity = await ResolveIdentity(code);
            var user = _externalAccountLinker.FindByExternalLogin(identity.Provider, identity.LoginProviderId);
            if (user == null)
            {
                throw new BusinessException(
                    GeexExceptionType.NotFound,
                    message: "外部账号尚未关联本地用户, 请使用 resolveExternalLogin.");
            }

            return user;
        }
    }
}
