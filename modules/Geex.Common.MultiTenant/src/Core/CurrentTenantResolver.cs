using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Geex.Common.MultiTenant.Api;

using Microsoft.AspNetCore.Http;

namespace Geex.Common.MultiTenant.Core
{
    public class CurrentTenantResolver : ICurrentTenantResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentTenantResolver(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public string? Resolve()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            // 租户解析优先级 claimsPrinciple>query>header>cookie
            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                var tenantCode = _httpContextAccessor.HttpContext?.User.Identity?.FindTenantCode();
                return tenantCode;
            }

            if (request?.Query.TryGetValue("__tenant", out var queryTenant) == true)
            {
                return queryTenant.IsNullOrEmpty() ? null : queryTenant.ToString();
            }
            if (request?.Headers.TryGetValue("__tenant", out var headerTenant) == true)
            {
                return headerTenant.IsNullOrEmpty() ? null : headerTenant.ToString();
            }
            if (request?.Cookies.TryGetValue("__tenant", out var cookieTenant) == true)
            {
                return cookieTenant.IsNullOrEmpty() ? null : cookieTenant;
            }
            return default;
        }
    }
}
