using Geex.Extensions.Authentication.Core.Entities;
using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record RegisterAndLinkExternalLoginRequest : IRequest<UserSession>
    {
        public string AccountLinkToken { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Nickname { get; set; }
    }
}
