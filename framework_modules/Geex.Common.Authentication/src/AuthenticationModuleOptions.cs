﻿using Geex.Common.Abstractions;

namespace Geex.Common.Authentication
{
    public class AuthenticationModuleOptions : GeexModuleOption<AuthenticationModule>
    {
        /// <summary>
        /// set this when you need include auth
        /// </summary>
        public InternalAuthOptions InternalAuthOptions { get; set; }

    }

    public class InternalAuthOptions
    {
        public string? ValidIssuer { get; set; }
        public string? ValidAudience { get; set; }
        public string? SecurityKey { get; set; }
        public double TokenExpireInSeconds { get; set; } = 3600 * 24;
    }

    public class ExternalAuthOptions
    {
    }
}
