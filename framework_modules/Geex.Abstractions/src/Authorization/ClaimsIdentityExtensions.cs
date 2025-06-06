using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Geex;
using Volo.Abp;

// ReSharper disable once CheckNamespace
namespace System.Security.Claims;

public static class ClaimsIdentityExtensions
{
    public static string? FindUserId(this ClaimsPrincipal principal)
    {
        return principal.Identity?.FindUserId();
    }

    public static string? FindClientId(this ClaimsPrincipal principal)
    {
        return principal.Identity?.FindClientId();
    }

    public static string? FindUserId(this IIdentity identity)
    {
        Check.NotNull(identity, nameof(identity));
        Claim? claim;
        if (!(identity is ClaimsIdentity claimsIdentity))
        {
            claim = null;
        }
        else
        {
            IEnumerable<Claim> claims = claimsIdentity.Claims;
            claim = claims != null
                ? claims.FirstOrDefault((Func<Claim, bool>)(c => c.Type == GeexClaimType.Sub))
                : null;
        }

        return claim?.Value;
    }

    public static string? FindClientId(this IIdentity identity)
    {
        Check.NotNull(identity, nameof(identity));
        Claim? claim;
        if (!(identity is ClaimsIdentity claimsIdentity))
        {
            claim = null;
        }
        else
        {
            IEnumerable<Claim> claims = claimsIdentity.Claims;
            claim = claims != null
                ? claims.FirstOrDefault((Func<Claim, bool>)(c => c.Type == "client_id"))
                : null;
        }

        return claim?.Value;
    }

    public static Guid? FindEditionId(this ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
        //Check.NotNull(principal, nameof(principal));
        //IEnumerable<Claim> claims = principal.Claims;
        //Claim claim = claims != null
        //    ? claims.FirstOrDefault((Func<Claim, bool>) (c => c.Type == GeexClaimType.EditionId))
        //    : null;
        //return claim == null || claim.Value.IsNullOrWhiteSpace() ? new Guid?() : Guid.Parse(claim.Value);
    }

    public static Guid? FindEditionId(this IIdentity identity)
    {
        throw new NotImplementedException();
        //Check.NotNull(identity, nameof(identity));
        //Claim claim1;
        //if (!(identity is ClaimsIdentity claimsIdentity))
        //{
        //    claim1 = null;
        //}
        //else
        //{
        //    IEnumerable<Claim> claims = claimsIdentity.Claims;
        //    claim1 = claims != null
        //        ? claims.FirstOrDefault((Func<Claim, bool>) (c => c.Type == GeexClaimType.EditionId))
        //        : null;
        //}

        //Claim claim2 = claim1;
        //return claim2 == null || claim2.Value.IsNullOrWhiteSpace() ? new Guid?() : Guid.Parse(claim2.Value);
    }
    /// <summary>
    /// 追加claim, 类型为array时会合并数组
    /// </summary>
    /// <param name="claimsIdentity"></param>
    /// <param name="claims"></param>
    public static void AppendClaims(this ClaimsIdentity claimsIdentity, params Claim[] claims)
    {
        AppendClaims(claimsIdentity, claims.AsEnumerable());
    }
    /// <summary>
    /// 追加claim, 类型为array时会合并数组
    /// </summary>
    /// <param name="claimsIdentity"></param>
    /// <param name="claims"></param>
    public static void AppendClaims(this ClaimsIdentity claimsIdentity, IEnumerable<Claim> claims)
    {
        foreach (var claim in claims)
        {
            if (claim.ValueType == "array" || !claimsIdentity.HasClaim(claim.Type, claim.Value))
            {
                claimsIdentity.AddClaim(claim);
            }
        }
    }
}