using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class SubscriptionAuthInterceptor : ISocketSessionInterceptor
    {
        // This is the key to the auth token in the HTTP Context
        public static readonly string HTTP_CONTEXT_WEBSOCKET_AUTH_KEY = "websocket-auth-token";
        // This is the key that apollo uses in the connection init request
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly IAuthenticationSchemeProvider _schemes;
        public TokenValidationParameters TokenValidationParameters { get; }

        public SubscriptionAuthInterceptor(TokenValidationParameters tokenValidationParameters, GeexJwtSecurityTokenHandler tokenHandler, IAuthenticationSchemeProvider schemes)
        {
            _schemes = schemes;
            _tokenHandler = tokenHandler;
            TokenValidationParameters = tokenValidationParameters;
        }

        /// <inheritdoc />
        public async ValueTask<ConnectionStatus> OnConnectAsync(ISocketSession session, IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var payload = (connectionInitMessage as InitializeConnectionMessage)?.Payload.GetValueOrDefault();
                var jwtHeader = payload?.GetString(HeaderNames.Authorization) ?? payload?.GetString(HeaderNames.Authorization.ToLowerInvariant());

                //if (string.IsNullOrEmpty(jwtHeader) || !jwtHeader.StartsWith("Bearer "))
                //    return ConnectionStatus.Reject("Unauthorized");

                if (!jwtHeader.IsNullOrEmpty())
                {
                    var authParts = jwtHeader.Split(' ', 2);
                    var schema = authParts[0];
                    var token = authParts[1];
                    var context = session.Connection.HttpContext;
                    context.Items[HTTP_CONTEXT_WEBSOCKET_AUTH_KEY] = token;
                    context.Request.Headers[HeaderNames.Authorization] = jwtHeader;
                    //var claimsPrincipal = _tokenHandler.ValidateToken(token, this.TokenValidationParameters, out var parsedToken);
                    //session.Connection.HttpContext.User = claimsPrincipal;

                    //session.Connection.ContextData.AddIfNotContains(new KeyValuePair<string, object>("HotChocolate.Authorization.UserState", new UserState(context.User, true)));

                    //context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                    //{
                    //    OriginalPath = context.Request.Path,
                    //    OriginalPathBase = context.Request.PathBase
                    //});
                    //OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
                    var result = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                    if (result?.Principal != null && result.Principal.Identity.IsAuthenticated)
                    {
                        context.User = result.Principal;
                        return ConnectionStatus.Accept();
                    }


                    //if (claims == null)
                    return ConnectionStatus.Reject("Unauthoized(invalid token)");
                }

                //// Grab our User ID
                //var userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

                //// Add it to our HttpContext
                //connection.HttpContext.Items["userId"] = userId;

                // Accept the websocket connection
                return ConnectionStatus.Reject("Unauthoized(no token provided)");
            }
            catch (Exception ex)
            {
                // If we catch any exceptions, reject the connection.
                // This is probably not ideal, there is likely a way to return a message
                // but I didn't look that far into it.
                return ConnectionStatus.Reject(ex.Message);
            }
        }

        /// <inheritdoc />
        public async ValueTask OnRequestAsync(ISocketSession session, string operationSessionId, IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken = new CancellationToken())
        {
        }

        /// <inheritdoc />
        public async ValueTask<IQueryResult> OnResultAsync(ISocketSession session, string operationSessionId, IQueryResult result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return result;
        }

        /// <inheritdoc />
        public async ValueTask OnCompleteAsync(ISocketSession session, string operationSessionId,
            CancellationToken cancellationToken = new CancellationToken())
        {
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<string, object>> OnPingAsync(ISocketSession session, IOperationMessagePayload pingMessage,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return pingMessage.As<IReadOnlyDictionary<string, object>>();
        }

        /// <inheritdoc />
        public async ValueTask OnPongAsync(ISocketSession session, IOperationMessagePayload pongMessage,
            CancellationToken cancellationToken = new CancellationToken())
        {
        }

        /// <inheritdoc />
        public async ValueTask OnCloseAsync(ISocketSession session, CancellationToken cancellationToken = new CancellationToken())
        {
        }
    }
}
