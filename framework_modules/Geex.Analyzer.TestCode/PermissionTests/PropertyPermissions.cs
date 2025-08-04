namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：属性初始化器中的权限字符串
    public class PropertyPermissionsTest : AppPermission<PropertyPermissionsTest>
    {
        public PropertyPermissionsTest(string value) : base(value) { }

        // 正确的属性权限 - 三段格式
        public static PropertyPermissionsTest ValidProperty { get; } = new("test_mutation_validProperty");

        // 应该报告 GEEX004
        public static PropertyPermissionsTest InvalidProperty { get; } = new("invalid");
    }
}
