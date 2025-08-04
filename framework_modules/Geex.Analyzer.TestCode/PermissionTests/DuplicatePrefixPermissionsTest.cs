namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：正确的权限字符串格式
    public class DuplicatePrefixPermissionsTest : AppPermission<DuplicatePrefixPermissionsTest>
    {
        public const string Prefix = "test";

        public DuplicatePrefixPermissionsTest(string value) : base($"{Prefix}_{value}")
        {
        }

        // 错误的权限字符串 - 构造函数中重复增加了前缀
        //public static DuplicatePrefixPermissionsTest Query { get; } = new("test_query_users");
        //public static DuplicatePrefixPermissionsTest Delete { get; } = new("test_mutation_deleteUser");
    }

    public class DuplicatePrefixPermissionsTest2 : AppPermission<DuplicatePrefixPermissionsTest2>
    {
        public const string Prefix = "test";

        public DuplicatePrefixPermissionsTest2(string value) : base($"test_{value}")
        {
        }

        // 错误的权限字符串 - 构造函数中重复增加了前缀
        public static DuplicatePrefixPermissionsTest2 Create { get; } = new($"test_{"mutation"}_createUser");
        public static DuplicatePrefixPermissionsTest2 Edit { get; } = new("test_mutation_editUser");
    }
}
