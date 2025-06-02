using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class GeexSecurityTokenDescriptor : SecurityTokenDescriptor, IHasId
    {
        public GeexSecurityTokenDescriptor(string sub, LoginProviderEnum provider, UserTokenGenerateOptions options, IEnumerable<Claim> customClaims = default)
        {
            if (options.Issuer != null) Issuer = options.Issuer;
            if (options.SigningCredentials != null) this.SigningCredentials = options.SigningCredentials;
            if (options.Audience != null) this.Audience = options.Audience;
            if (options.Expires.HasValue)
            {
                Expires = DateTime.Now.Add(options.Expires.Value);
            }
            IssuedAt = DateTime.Now;
            Subject = new ClaimsIdentity(new Claim[]
            {
                new GeexClaim(GeexClaimType.Sub, sub),
                new GeexClaim(GeexClaimType.Provider, provider),
            });
            if (customClaims?.Any() == true)
            {
                Subject.AppendClaims(customClaims);
            }
        }

        /// <inheritdoc />
        public string Id => this.Subject.FindUserId();
    }
}
