using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Extensions.Requests;
using Geex.Abstractions;
using Geex.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication
{
    public class IdsvrMiddleware : IMiddleware
    {
        public async Task Authorize(HttpContext HttpContext, RequestDelegate requestDelegate)
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the user principal stored in the authentication cookie.
            var result = await HttpContext.AuthenticateAsync();

            // If the user principal can't be extracted, redirect the user to the login page.
            if (!result.Succeeded)
            {
                await HttpContext.ChallengeAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = HttpContext.Request.PathBase + HttpContext.Request.Path + QueryString.Create(
                            HttpContext.Request.HasFormContentType ? HttpContext.Request.Form.ToList() : HttpContext.Request.Query.ToList())
                    });
                return;
            }

            // Create a new claims principal
            var claims = result.Principal.Claims;

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Set requested scopes (this is not done automatically)
            claimsPrincipal.SetScopes(request.GetScopes());

            // Signing in with the OpenIddict authentiction scheme trigger OpenIddict to issue a code (which can be exchanged for an access token)
            await HttpContext.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }

        /// <inheritdoc />
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
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
                throw new InvalidOperationException("Authentication failed.");
            }

            // Create a new claims principal with existing claims
            var claims = result.Principal.Claims;
            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Set requested scopes
            claimsPrincipal.SetScopes(request.GetScopes());

            // Sign in the user
            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
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
                return;
            }

            await next(context);
        }

        private async Task UserInfo(HttpContext HttpContext, RequestDelegate requestDelegate)
        {
            ClaimsPrincipal? claimsPrincipal;
            if (HttpContext.User.Identity?.IsAuthenticated == true && HttpContext.User.FindUserId() != null)
            {
                claimsPrincipal = HttpContext.User;
            }
            else
            {
                claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }

            var claims = claimsPrincipal.Claims;
            var users = HttpContext.RequestServices.GetService<IUnitOfWork>().Query<IUser>();
            var user = users.FirstOrDefault(x => x.Id == claimsPrincipal.FindUserId());
            if (user != null)
            {
                claims = claims.Concat(new[]
                {
                    new GeexClaim(GeexClaimType.Nickname, user.Nickname??user.Username),
                    new GeexClaim(GeexClaimType.Org, user.OrgCodes.JoinAsString(",")),
                    new GeexClaim(GeexClaimType.Role, user.RoleIds.JoinAsString(",")),
                    new GeexClaim(GeexClaimType.Tenant, user.TenantCode??""),
                    new GeexClaim(GeexClaimType.Provider, user.LoginProvider),
                });
            }

            await HttpContext.Response.WriteAsJsonAsync(claims.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => string.Join(',', x.Select(y => y.Value))));
        }

        private async Task Token(HttpContext HttpContext, RequestDelegate requestDelegate)
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
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

                claimsPrincipal.SetScopes(request.GetScopes());
            }
            else if (request.IsAuthorizationCodeGrantType())
            //{
            //    claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            //}
            //else if (request.IsRefreshTokenGrantType())
            {
                // Retrieve the claims principal stored in the refresh token.
                claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }
            await HttpContext.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
    }
}
