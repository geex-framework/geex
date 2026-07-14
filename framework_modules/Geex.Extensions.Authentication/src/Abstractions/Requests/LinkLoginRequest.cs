using Geex.Extensions.Authentication.Core.Entities;
using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record LinkLoginRequest : IRequest<UserSession>
    {
        public string UserLoginLinkToken { get; set; }
    }
}
