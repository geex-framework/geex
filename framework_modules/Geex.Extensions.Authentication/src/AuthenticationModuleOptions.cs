﻿using Geex.Abstractions;

namespace Geex.Extensions.Authentication
{
    public class AuthenticationModuleOptions : GeexModuleOption<AuthenticationModule>
    {
        public string? ValidAudience { get; set; }
        public double TokenExpireInSeconds { get; set; } = 3600 * 24;
    }

    public class ExternalAuthOptions
    {
    }
}
