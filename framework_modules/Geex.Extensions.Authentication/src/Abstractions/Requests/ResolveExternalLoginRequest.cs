using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record ResolveExternalLoginRequest : IRequest<ResolveExternalLoginResult>
    {
        public LoginProviderEnum LoginProvider { get; set; }
        public string Code { get; set; }
    }
}
