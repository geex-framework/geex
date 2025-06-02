using System;
using Microsoft.IdentityModel.Tokens;

namespace Geex.Extensions.Authentication.Core.Utils;

public record UserTokenGenerateOptions
{
    public string? Issuer;
    public string? Audience;
    public TimeSpan? Expires;
    public SigningCredentials? SigningCredentials;

    public UserTokenGenerateOptions(string? issuer, string audience, SigningCredentials? signingCredentials, TimeSpan? expires)
    {
        this.Issuer = issuer;
        this.Audience = audience;
        this.Expires = expires;
        this.SigningCredentials = signingCredentials;
    }
}