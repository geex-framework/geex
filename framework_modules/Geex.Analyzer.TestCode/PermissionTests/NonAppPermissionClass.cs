namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：非 AppPermission 类 - 不应该被分析
    public class RegularClassTest
    {
        // 这些不应该被分析，因为不继承自 AppPermission
        public static string InvalidPermission { get; } = "invalid";
        public static string AnotherInvalid { get; } = "also_invalid";

        public RegularClassTest(string value)
        {
        }
    }
}
