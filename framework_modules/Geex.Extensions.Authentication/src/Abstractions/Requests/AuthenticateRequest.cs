using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record AuthenticateRequest : IRequest<UserSession>
    {
        public string UserIdentifier { get; set; }
        public string Password { get; set; }
    }
}
