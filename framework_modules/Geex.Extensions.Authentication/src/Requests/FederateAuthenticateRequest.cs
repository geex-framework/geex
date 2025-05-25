using Geex.Abstractions;
using Geex.Extensions.Authentication.Domain;
using MediatR;

namespace Geex.Extensions.Requests.Authentication
{
    public record FederateAuthenticateRequest : IRequest<UserToken>
    {
        /// <summary>
        /// 登陆提供方
        /// </summary>
        public LoginProviderEnum LoginProvider { get; set; }
        /// <summary>
        /// OAuth Code
        /// </summary>
        public string Code { get; set; }
    }
}
