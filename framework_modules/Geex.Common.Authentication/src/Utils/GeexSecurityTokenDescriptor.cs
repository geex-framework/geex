using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstractions;
using Geex.Common.Abstractions.Enumerations;
using Geex.Common.Authentication.Domain;

using Microsoft.IdentityModel.Tokens;

namespace Geex.Common.Authentication.Utils
{
    public class GeexSecurityTokenDescriptor : SecurityTokenDescriptor, IHasId
    {
        public GeexSecurityTokenDescriptor(IUser user, LoginProviderEnum provider, UserTokenGenerateOptions options, IEnumerable<Claim> customClaims = default)
        {
            this.Audience = options.Audience;
            this.SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)), SecurityAlgorithms.HmacSha256Signature);
            if (options.Expires.HasValue)
            {
                Expires = DateTime.Now.Add(options.Expires.Value);
            }
            IssuedAt = DateTime.Now;
            Issuer = options.Issuer;
            Subject = new ClaimsIdentity(new Claim[]
            {
                new GeexClaim(GeexClaimType.Sub, user.Id),
                new GeexClaim(GeexClaimType.Provider, provider),
            });
            if (user.TenantCode != null)
            {
                Subject.AddClaim(new GeexClaim(GeexClaimType.Tenant, user.TenantCode));
            }
            if (customClaims?.Any() == true)
            {
                Subject.AppendClaims(customClaims);
            }
        }

        /// <inheritdoc />
        public string Id => this.Subject.FindUserId();
    }
}