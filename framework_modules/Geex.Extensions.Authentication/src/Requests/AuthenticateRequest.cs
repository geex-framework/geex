using Geex.Extensions.Authentication.Domain;
using MediatR;

namespace Geex.Extensions.Requests.Authentication
{
    public record AuthenticateRequest : IRequest<UserToken>
    {
        public string UserIdentifier { get; set; }
        public string Password { get; set; }
    }
}
