using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Enumerations;

using Microsoft.IdentityModel.Tokens;

namespace Geex.Common.Authentication.Utils
{
    public class GeexJwtSecurityTokenHandler : JwtSecurityTokenHandler, ISecurityTokenValidator
    {
        public GeexJwtSecurityTokenHandler()
        {
        }

        public new string DecryptToken(JwtSecurityToken token, TokenValidationParameters @params)
        {
            return base.DecryptToken(token, @params);
        }

        public override ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            var principal = base.ValidateToken(securityToken, validationParameters, out validatedToken);
            //if (validatedToken.ValidFrom > DateTime.Now && validatedToken.ValidTo < DateTime.Now)
            //{
            //    throw new SecurityTokenExpiredException($"token is only valid in {validatedToken.ValidFrom} ~ {validatedToken.ValidTo}");
            //}
            //var subClaim = principal.Claims.First(c => c.Type == GeexClaimType.Sub);
            //var expireClaim = principal.Claims.First(x => x.Type == GeexClaimType.Expires);
            return principal;
        }

        /// <inheritdoc />
        public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            var result = await base.ValidateTokenAsync(token, validationParameters);
            return result;
        }
    }

}
