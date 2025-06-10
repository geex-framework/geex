using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class IdsvrMiddleware : IMiddleware
    {
        public async Task Authorize(HttpContext context, RequestDelegate requestDelegate)
        {
            var request = context.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
            // Retrieve the user principal stored in the authentication cookie.
            //var cliamsTrans = context.RequestServices.GetRequiredService<IClaimsTransformation>();

            if (!request.AccessToken.IsNullOrEmpty())
            {
                var result = await context.AuthenticateAsync(LocalAuthHandler.SchemeName);
                if (!result.Succeeded)
                {
                    await Challenge(context);
                    return;
                }
                context.User = result.Principal;
            }

            if (context.User.Identity is { IsAuthenticated: true })
            {
                var existedClaimsPrincipal = context.User;
                // Set requested scopes for the validated principal
                existedClaimsPrincipal.SetScopes(request.GetScopes());

                // Sign in to both OpenIddict and Cookie schemes to maintain login state
                await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, existedClaimsPrincipal);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, existedClaimsPrincipal);
                return;
            }
            await Challenge(context);
        }

        private static async Task Challenge(HttpContext context)
        {
            await context.ChallengeAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = context.Request.PathBase + context.Request.Path + QueryString.Create(
                        context.Request.HasFormContentType ? context.Request.Form.ToList() : context.Request.Query.ToList())
                });
        }

        /// <inheritdoc />
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // checksession is now handled separately, no need to check here

            var path = context.GetOpenIddictServerEndpointType();
            switch (path)
            {
                case OpenIddictServerEndpointType.Authorization:
                    await this.Authorize(context, next);
                    break;
                case OpenIddictServerEndpointType.Unknown:
                    break;
                case OpenIddictServerEndpointType.Token:
                    await this.Token(context, next);
                    break;
                case OpenIddictServerEndpointType.Logout:
                    await this.Logout(context, next);
                    break;
                case OpenIddictServerEndpointType.Configuration:
                    break;
                case OpenIddictServerEndpointType.Cryptography:
                    break;
                case OpenIddictServerEndpointType.Userinfo:
                    await this.UserInfo(context, next);
                    break;
                case OpenIddictServerEndpointType.Introspection:
                    break;
                case OpenIddictServerEndpointType.Revocation:
                    break;
                case OpenIddictServerEndpointType.Device:
                    break;
                case OpenIddictServerEndpointType.Verification:
                    await this.Verification(context, next);
                    break;
                default:
                    await next(context);
                    break;
            }
        }

        private async Task Verification(HttpContext context, RequestDelegate next)
        {
            var request = context.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the user principal stored in the authentication cookie
            var result = await context.AuthenticateAsync();

            if (!result.Succeeded)
            {
                await Challenge(context);
            }

            // Create a new claims principal with existing claims
            var claims = result.Principal.Claims;
            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var cliamsTrans = context.RequestServices.GetRequiredService<IClaimsTransformation>();
            await cliamsTrans.TransformAsync(claimsPrincipal);
            // Set requested scopes
            claimsPrincipal.SetScopes(request.GetScopes());

            // Sign in the user
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
        }

        private async Task Logout(HttpContext context, RequestDelegate next)
        {
            var request = context.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Revoke authentication cookie
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Revoke OpenID tokens
            await context.SignOutAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Redirect to post_logout_redirect_uri if provided
            if (request.PostLogoutRedirectUri != null)
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = request.PostLogoutRedirectUri
                };
                context.Response.Redirect(request.PostLogoutRedirectUri);
                return;
            }

            await next(context);
        }

        private async Task UserInfo(HttpContext context, RequestDelegate requestDelegate)
        {
            ClaimsPrincipal? claimsPrincipal;
            if (context.User.Identity?.IsAuthenticated == true && context.User.FindUserId() != null)
            {
                claimsPrincipal = context.User;
            }
            else
            {
                claimsPrincipal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }

            var claims = claimsPrincipal.Claims;
            claimsPrincipal = await context.RequestServices.GetService<IClaimsTransformation>().TransformAsync(claimsPrincipal);

            await context.Response.WriteAsJsonAsync(claimsPrincipal.Claims.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => string.Join(',', x.Select(y => y.Value))));
        }

        private async Task Token(HttpContext context, RequestDelegate requestDelegate)
        {
            var request = context.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            ClaimsPrincipal claimsPrincipal = ClaimsPrincipal.Current;

            if (request.IsClientCredentialsGrantType())
            {
                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // Subject (sub) is a required field, we use the client id as the subject identifier here.
                identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId ?? throw new InvalidOperationException());

                // Add some claim, don't forget to add destination otherwise it won't be added to the access token.
                //identity.AddClaim(OpenIddictConstants.Claims., "some-value", OpenIddictConstants.Destinations.AccessToken);

                claimsPrincipal = new ClaimsPrincipal(identity);
                var cliamsTrans = context.RequestServices.GetRequiredService<IClaimsTransformation>();
                await cliamsTrans.TransformAsync(claimsPrincipal);
                claimsPrincipal.SetScopes(request.GetScopes());
            }
            else if (request.IsAuthorizationCodeGrantType())
            {
                claimsPrincipal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

                // Also sign in to Cookie scheme to maintain login state
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
            }
            else if (request.IsRefreshTokenGrantType())
            {
                // Retrieve the claims principal stored in the refresh token.
                claimsPrincipal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

                // Also sign in to Cookie scheme to maintain login state
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
            }

            // Use OpenIddict authentication scheme to generate and return tokens
            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
    }
}
