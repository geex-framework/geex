namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：多个无效权限字符串
    public class MultipleInvalidPermissionsTest : AppPermission<MultipleInvalidPermissionsTest>
    {
        // 构造函数中的无效权限 - 只有两段
        public MultipleInvalidPermissionsTest() : base("invalid_permission") { }

        public MultipleInvalidPermissionsTest(string value) : base(value) { }

        // InvalidPermissions 类中的多个无效权限
        public static MultipleInvalidPermissionsTest Invalid1 { get; } = new("invalid1");
        public static MultipleInvalidPermissionsTest Invalid2 { get; } = new("too_many_parts_here_invalid");

        // 字段中的无效权限
        public static readonly MultipleInvalidPermissionsTest InvalidField = new("invalidField");

        // 有效权限示例 - 三段格式
        public static MultipleInvalidPermissionsTest Valid { get; } = new("test_mutation_validAction");
    }
}
