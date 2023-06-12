using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.Authentication;
using Geex.Common.Identity.Core.Aggregates.Users;

using IdentityModel;
using IdentityModel.Client;

using MediatR;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using MongoDB.Entities;

namespace Geex.Bms.Core.Authentication.LoginProviders
{
    /// <summary>
    /// Geex登陆Provider
    /// </summary>
    public class GeexLoginProvider : IExternalLoginProvider
    {
        private readonly GeexLoginConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly Task<DiscoveryDocumentResponse> _disc;
        private readonly ILogger<GeexLoginProvider> _logger;
        private readonly DbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly ICurrentTenant _currentTenant;

        public LoginProviderEnum Provider { get; } = BmsLoginProviderEnum.Geex;

        public GeexLoginProvider(ILogger<GeexLoginProvider> logger, IConfiguration configuration, DbContext dbContext, IMediator mediator, ICurrentTenant currentTenant)
        {
            this._configuration = new GeexLoginConfiguration();
            configuration.GetSection("Authentication:External:Geex").Bind(this._configuration);
            _logger = logger;
            this._dbContext = dbContext;
            this._mediator = mediator;
            _currentTenant = currentTenant;
            if (Uri.TryCreate(_configuration.Authority, UriKind.RelativeOrAbsolute, out var baseAddress))
            {
                this._httpClient = new HttpClient()
                {
                    BaseAddress = baseAddress
                };
                this._disc = this._httpClient.GetDiscoveryDocumentAsync();
            }
        }
        /// <summary>
        /// token换取用户信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        private async Task<ClaimsIdentity> Token2UserInfo(string token)
        {
            var disc = await _disc;
            var userInfo = await this._httpClient.GetUserInfoAsync(new UserInfoRequest()
            {
                RequestUri = new Uri(disc.UserInfoEndpoint),
                ClientId = _configuration.ClientId,
                ClientSecret = _configuration.ClientSecret,
                Token = token,
            });

            if (userInfo.IsError)
            {
                throw new BusinessException(GeexExceptionType.Unknown, message: "使用Code换取登陆令牌时请求失败:" + Environment.NewLine + userInfo.Error);
            }
            return new ClaimsIdentity(userInfo.Claims);
        }

        private async Task<User> CreateOrUpdateUserByOpenId(ClaimsIdentity extIdentity)
        {
            var openId = extIdentity.FindUserId();
            if (string.IsNullOrEmpty(openId))
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "用户信息格式不正确, 缺少openId." + Environment.NewLine + extIdentity.ToJsonSafe());
            }
            User? user = _dbContext.Queryable<User>()
                  .SingleOrDefault(x => x.LoginProvider == Provider && x.OpenId == openId);

            if (user == default)
            {
                // create user

                // or throw error
                //throw new BusinessException(GeexExceptionType.NotFound,
                //    message: $"用户[{openId}]在租户[{_currentTenant.Code}]上不存在." + Environment.NewLine +
                //             extIdentity.Claims.ToDictionary(x => x.Type, x => x.Value).ToJsonSafe());
            }
            else
            {
                // update user info
            }

            return user;
        }

        /// <inheritdoc />
        public async Task<IUser> ExternalLogin(string accessToken)
        {
            var extUserInfo = await Token2UserInfo(accessToken);
            var userInfo = await CreateOrUpdateUserByOpenId(extUserInfo);
            return userInfo;
        }

        private async Task<string> Code2Token(string accessCode)
        {
            var disc = await _disc;
            var token = await this._httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest()
            {
                RequestUri = new Uri(disc.TokenEndpoint),
                ClientId = _configuration.ClientId,
                ClientSecret = _configuration.ClientSecret,
                GrantType = OidcConstants.GrantTypes.ClientCredentials,
                Code = accessCode,
                RedirectUri = _configuration.RedirectUri,
            });

            if (token.IsError)
            {
                throw new BusinessException(GeexExceptionType.Unknown, message: "使用Code换取登陆令牌时请求失败:" + Environment.NewLine + token.Error);
            }

            return token.AccessToken;
        }
    }
    /// <summary>
    /// kufore登陆配置
    /// </summary>
    public class GeexLoginConfiguration
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// <example>bms</example>
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// <example>bms</example>
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// <example>https://bms.local.geex.cn/passport/callback</example>
        /// </summary>
        public string RedirectUri { get; set; }
        /// <summary>
        /// 单点认证中心基地址
        /// </summary>
        public string Authority { get; set; }
    }
}
