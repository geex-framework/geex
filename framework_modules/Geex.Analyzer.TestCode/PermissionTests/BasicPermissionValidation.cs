namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：基本的权限验证
    public class BasicPermissionValidationTest : AppPermission<BasicPermissionValidationTest>
    {
        public BasicPermissionValidationTest(string value) : base(value) { }

        // 正确的权限格式 - 三段格式
        public static BasicPermissionValidationTest Read { get; } = new("test_query_read");
        public static BasicPermissionValidationTest Write { get; } = new("test_mutation_write");
    }
}
