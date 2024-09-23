using Geex.Common.Authentication.Domain;
using MediatR;

namespace Geex.Common.Requests.Authentication
{
    public record AuthenticateRequest : IRequest<UserToken>
    {
        public string UserIdentifier { get; set; }
        public string Password { get; set; }
    }
}