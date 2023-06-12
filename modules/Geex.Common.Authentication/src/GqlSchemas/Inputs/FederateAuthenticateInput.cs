using Geex.Common.Abstraction;

namespace Geex.Common.Authentication.GqlSchemas.Inputs
{
    public class FederateAuthenticateInput
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
