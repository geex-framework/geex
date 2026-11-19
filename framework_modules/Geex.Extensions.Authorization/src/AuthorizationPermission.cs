using System;
using JetBrains.Annotations;

namespace Geex.Extensions.Authorization
{
    public class AuthorizationPermission : AppPermission<AuthorizationPermission>
    {
        public AuthorizationPermission([NotNull] string value) : base($"{typeof(AuthorizationPermission).DomainName()}_{value}")
        {
        }

        internal AuthorizationPermission(string name, string value) : base(name, value)
        {
        }

        public static AuthorizationPermission Authorize { get; } = new AuthorizationPermission("mutation_authorize");
    }
}
