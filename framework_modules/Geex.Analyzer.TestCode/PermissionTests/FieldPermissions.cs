namespace Geex.Analyzer.TestCode.PermissionTests
{
    // 测试用例：字段声明中的权限字符串
    public class FieldPermissionsTest : AppPermission<FieldPermissionsTest>
    {
        public FieldPermissionsTest(string value) : base(value) { }

        // 应该报告 GEEX005 (字段不允许) + GEEX004 (格式错误，只有两部分)
        public static readonly FieldPermissionsTest InvalidQueryField = new("query_invalidField");

        // 应该报告 GEEX005 (字段不允许) + GEEX004 (格式错误)
        public static readonly FieldPermissionsTest InvalidField = new("invalid");
    }
}
