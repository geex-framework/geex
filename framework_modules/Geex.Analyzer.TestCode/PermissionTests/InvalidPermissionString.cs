

namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：无效的权限字符串格式
    public class InvalidPermissionsTest : AppPermission<InvalidPermissionsTest>
    {
        public InvalidPermissionsTest(string value) : base(value)
        {
        }

        public static InvalidPermissionsTest Valid { get; } = new("module_object_field");
        // 应该报告 GEEX004 - 格式错误
        public static InvalidPermissionsTest Invalid1 { get; } = new("invalid");
        // 应该报告 GEEX004 - 格式错误
        public static InvalidPermissionsTest Invalid3 { get; } = new("too_many_parts_here_invalid");
        // 应该报告 GEEX004 - 格式错误
        public static InvalidPermissionsTest Invalid4 { get; } = new("");
    }
}
