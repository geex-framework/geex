namespace Geex.Extensions.Authentication
{
    public class AuthenticationModuleOptions : GeexModuleOption<AuthenticationModule>
    {
        public string? ValidAudience { get; set; }
        public double TokenExpireInSeconds { get; set; } = 3600*4;
    }

    public class ExternalAuthOptions
    {
    }
}
