using System;
using Geex.Common.Authorization;
using JetBrains.Annotations;

namespace Geex.Common.Identity
{
    public class IdentityPermission : AppPermission<IdentityPermission>
    {

        public IdentityPermission(string value) : base($"identity_{value}")
        {
        }
        public class UserPermission : IdentityPermission
        {
            public static UserPermission Query { get; } = new("query_users");
            public static UserPermission Create { get; } = new("mutation_createUser");
            public static UserPermission Edit { get; } = new("mutation_editUser");

            public UserPermission([NotNull] string value) : base(value)
            {
            }
        }
        public class RolePermission : IdentityPermission
        {
            public static RolePermission Query { get; } = new("query_roles");
            public static RolePermission Create { get; } = new("mutation_createRole");
            public static RolePermission Edit { get; } = new("mutation_editRole");

            public RolePermission([NotNull] string value) : base(value)
            {
            }
        }
        public class OrgPermission : IdentityPermission
        {
            public static OrgPermission Create { get; } = new("mutation_createOrg");
            public static OrgPermission Edit { get; } = new("mutation_editOrg");

            public OrgPermission([NotNull] string value) : base(value)
            {
            }
        }
    }
}
