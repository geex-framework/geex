using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record AuthenticateRequest : IRequest<UserToken>
    {
        public string UserIdentifier { get; set; }
        public string Password { get; set; }
    }
}
