namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：复杂的权限字符串结构
    public class IdentityPermissionsTest : AppPermission<IdentityPermissionsTest>
    {
        public const string Prefix = "identity";

        public IdentityPermissionsTest(string value) : base($"{Prefix}_{value}")
        {
        }

        public class UserPermissionsTest : IdentityPermissionsTest
        {
            // 正确的权限字符串
            public static UserPermissionsTest Query { get; } = new("query_users");
            public static UserPermissionsTest Create { get; } = new("mutation_createUser");
            public static UserPermissionsTest Edit { get; } = new("mutation_editUser");
            public static UserPermissionsTest Delete { get; } = new("mutation_deleteUser");

            public UserPermissionsTest(string value) : base(value)
            {
            }
        }

        public class RolePermissionsTest : IdentityPermissionsTest
        {
            // 正确的权限字符串
            public static RolePermissionsTest Query { get; } = new("query_roles");
            public static RolePermissionsTest Assign { get; } = new("mutation_assignRole");

            public RolePermissionsTest(string value) : base(value)
            {
            }
        }
    }
}
