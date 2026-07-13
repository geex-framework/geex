using Geex.Extensions.Authentication.Core.Entities;
using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record LinkExternalLoginRequest : IRequest<UserSession>
    {
        public string AccountLinkToken { get; set; }
    }
}
