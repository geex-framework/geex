using Geex.Abstractions;
using Geex.Common.Authentication.Domain;
using MediatR;

namespace Geex.Common.Requests.Authentication
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
