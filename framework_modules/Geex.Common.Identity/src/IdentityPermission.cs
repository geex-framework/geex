using System;
using Geex.Common.Authorization;
using JetBrains.Annotations;

namespace Geex.Common.Identity
{
    public class IdentityPermission : AppPermission<IdentityPermission>
    {

        public IdentityPermission(string value) : base($"{typeof(IdentityPermission).DomainName()}.{value}")
        {
        }
        public class UserPermission : IdentityPermission
        {
            public static UserPermission Query { get; } = new("query.users");
            public static UserPermission Create { get; } = new("mutation.createUser");
            public static UserPermission Edit { get; } = new("mutation.editUser");

            public UserPermission([NotNull] string value) : base(value)
            {
            }
        }
        public class RolePermission : IdentityPermission
        {
            public static RolePermission Query { get; } = new("query.roles");
            public static RolePermission Create { get; } = new("mutation.createRole");
            public static RolePermission Edit { get; } = new("mutation.editRole");

            public RolePermission([NotNull] string value) : base(value)
            {
            }
        }
        public class OrgPermission : IdentityPermission
        {
            public static OrgPermission Query { get; } = new("query.orgs");
            public static OrgPermission Create { get; } = new("mutation.createOrg");
            public static OrgPermission Edit { get; } = new("mutation.editOrg");

            public OrgPermission([NotNull] string value) : base(value)
            {
            }
        }
    }
}
