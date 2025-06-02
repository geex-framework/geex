using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class GeexClaimsTransformation : IClaimsTransformation
    {
        private readonly IEnumerable<ISubClaimsTransformation> _transformations;
        private readonly IUnitOfWork _uow;
        private readonly IRedisDatabase _redis;
        private UserTokenGenerateOptions _options;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private TokenValidationParameters _validationParams;

        public GeexClaimsTransformation(IEnumerable<ISubClaimsTransformation> transformations, IUnitOfWork uow, IRedisDatabase redis, UserTokenGenerateOptions options, GeexJwtSecurityTokenHandler tokenHandler, TokenValidationParameters validationParams)
        {
            _transformations = transformations;
            _uow = uow;
            _redis = redis;
            _options = options;
            _tokenHandler = tokenHandler;
            _validationParams = validationParams;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindUserId();
            if (userId.IsNullOrEmpty())
            {
                return principal;
            }

            ClaimsIdentity claimsIdentity = new ClaimsIdentity();
            // todo:单点直接登陆会导致缓存未清理, 暂时没找到方案, 这里临时禁用
            //var cachedSession = await this._redis.GetNamedAsync<UserSessionCache>(userId);
            //if (cachedSession != default)
            //{
            //    claimsIdentity.AppendClaims((_tokenHandler.ReadToken(cachedSession.token) as JwtSecurityToken).Claims);
            //    principal.AddIdentity(claimsIdentity);
            //    return principal;
            //}

            var user = _uow.Query<IAuthUser>().GetById(userId);
            if (user == null)
            {
                return principal;
            }

            foreach (var transformation in this._transformations)
            {
                var claimsPrincipal = await transformation.TransformAsync(user, principal);
                claimsIdentity.AppendClaims(claimsPrincipal.Claims);
            }

            var tokenDescriptor = new GeexSecurityTokenDescriptor(userId, LoginProviderEnum.Local, _options, claimsIdentity.Claims);
            // 设置用户session, 缓存数据10分钟, 避免大量的组织架构和权限查询
            await this._redis.SetNamedAsync(new UserSessionCache { userId = userId, token = _tokenHandler.CreateEncodedJwt(tokenDescriptor) }, expireIn: TimeSpan.FromMinutes(10));
            principal.AddIdentity(claimsIdentity);

            return principal;
        }
    }
}
