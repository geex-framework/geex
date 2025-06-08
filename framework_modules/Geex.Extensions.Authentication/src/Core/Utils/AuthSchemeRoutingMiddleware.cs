//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using OpenIddict.Abstractions;
//using OpenIddict.Client.AspNetCore;

//namespace Geex.Extensions.Authentication.Core.Utils
//{
//    // 认证方案路由中间件
//    internal class AuthSchemeRoutingMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly ILogger<AuthSchemeRoutingMiddleware> _logger;

//        public AuthSchemeRoutingMiddleware(RequestDelegate next, ILogger<AuthSchemeRoutingMiddleware> logger)
//        {
//            _next = next;
//            _logger = logger;
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            var schemes = GetAuthenticationSchemesFromRequest(context);

//            if (schemes.Any())
//            {
//                context.Items["AuthenticationSchemes"] = schemes;
//                _logger.LogDebug($"Available authentication schemes: {string.Join(", ", schemes)}");
//            }

//            await _next(context);
//        }

//        private List<string> GetAuthenticationSchemesFromRequest(HttpContext context)
//        {
//            var schemes = new List<string>();

//            // 1. 检查 Authorization header（优先级最高）
//            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
//            if (!string.IsNullOrEmpty(authHeader))
//            {
//                var headerScheme = GetSchemeFromAuthorizationHeader(authHeader);
//                if (!string.IsNullOrEmpty(headerScheme))
//                {
//                    schemes.Add(headerScheme);
//                    return schemes; // Authorization header 存在时，优先使用
//                }
//            }

//            // 2. 检查 OpenIddict Cookie
//            if (HasOpenIddictCookie(context))
//            {
//                schemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
//            }

//            return schemes;
//        }

//        private string GetSchemeFromAuthorizationHeader(string authHeader)
//        {
//            if (authHeader.StartsWith("SuperAdmin ", StringComparison.OrdinalIgnoreCase))
//                return SuperAdminAuthHandler.SchemeName;

//            if (authHeader.StartsWith("Local ", StringComparison.OrdinalIgnoreCase))
//                return LocalAuthHandler.SchemeName;

//            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
//                return JwtBearerDefaults.AuthenticationScheme;

//            return null;
//        }

//        private bool HasOpenIddictCookie(HttpContext context)
//        {
//            // OpenIddict 通常使用以下 Cookie 名称模式
//            var openIddictCookiePatterns = new[]
//            {
//                ".AspNetCore.OpenIddict.Server.Client",  // OpenIddict Client Cookie
//                ".AspNetCore.OpenIddict.Server.Session", // OpenIddict Session Cookie
//                ".AspNetCore.Cookies",                   // 默认 ASP.NET Core Cookie
//                "oidc_session",                          // 常见的 OIDC session cookie
//                "openiddict_session"                     // 自定义 OpenIddict session
//            };

//            // 检查确切匹配的 Cookie 名称
//            foreach (var pattern in openIddictCookiePatterns)
//            {
//                if (context.Request.Cookies.ContainsKey(pattern))
//                {
//                    return true;
//                }
//            }

//            // 检查包含特定模式的 Cookie
//            return context.Request.Cookies.Any(cookie =>
//                cookie.Key.Contains("OpenIddict", StringComparison.OrdinalIgnoreCase) ||
//                cookie.Key.Contains("oidc", StringComparison.OrdinalIgnoreCase) ||
//                cookie.Key.StartsWith(".AspNetCore.", StringComparison.OrdinalIgnoreCase));
//        }
//    }
//}
