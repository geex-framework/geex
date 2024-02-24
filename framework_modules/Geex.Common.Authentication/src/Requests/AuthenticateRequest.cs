using Geex.Common.Authentication.Domain;
using MediatR;

namespace Geex.Common.Authentication.Requests
{
    public class AuthenticateRequest : IRequest<UserToken>
    {
        public string UserIdentifier { get; set; }
        public string Password { get; set; }
    }
}