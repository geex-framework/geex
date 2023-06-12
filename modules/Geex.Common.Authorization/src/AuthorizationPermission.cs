using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Geex.Common.Authorization
{
    public class AuthorizationPermission : AppPermission<AuthorizationPermission>
    {
        public AuthorizationPermission([NotNull] string value) : base($"{typeof(AuthorizationPermission).DomainName()}_{value}")
        {
        }

        public static AuthorizationPermission Authorize { get; } = new AuthorizationPermission("mutation_authorize");
    }
}
