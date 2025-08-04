

namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：构造函数中的权限字符串验证
    public class ConstructorPermissionsTest : AppPermission<ConstructorPermissionsTest>
    {
        // 应该报告 GEEX004
        public ConstructorPermissionsTest() : base("invalid_permission")
        {
        }

        // 正确的构造函数
        public ConstructorPermissionsTest(string value) : base("module_" + value)
        {
        }

        // 正确格式的权限
        public static ConstructorPermissionsTest Valid { get; } = new("mutation_validAction");
    }
}
