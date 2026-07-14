using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record ResolveLoginRequest : IRequest<ResolveLoginResult>
    {
        public LoginProviderEnum LoginProvider { get; set; }
        public string Code { get; set; }
    }
}
