using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;
using Geex.Analyzer.Tests;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<PermissionStringAnalyzer>;

    /// <summary>
    /// PermissionStringAnalyzer 的测试类
    /// 使用 Geex.Analyzer.TestCode 项目中的真实代码进行测试
    /// </summary>
    public class PermissionStringAnalyzerTests
    {
        [Fact]
        public async Task DuplicatePrefixPermissionsTest_ShouldReportDiagnostic()
        {
            // DuplicatePrefixPermissionsTest.cs 中的权限字符串会导致重复前缀，应该报告诊断
            var expectedDiagnostics = new[]
            {
                //DiagnosticResultBuilder.Create("GEEX004")
                //    .WithArguments("PREFIX_test_query_users"),
                //DiagnosticResultBuilder.Create("GEEX004")
                    //.WithArguments("PREFIX_test_mutation_deleteUser"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("PREFIX_test_mutation_createUser"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("PREFIX_test_mutation_editUser"),
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/DuplicatePrefixPermissionsTest.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task InvalidPermissionString_ShouldReportDiagnostic()
        {
            // 测试无效的权限字符串格式应该报告 GEEX004 诊断
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("invalid"),

                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("too_many_parts_here_invalid"),

                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/InvalidPermissionString.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task PermissionStringInConstructor_ShouldValidate()
        {
            // 测试构造函数中的权限字符串验证
            var expected = DiagnosticResultBuilder.Create("GEEX004")
                .WithArguments("invalid_permission");

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/PermissionStringInConstructor.cs", expected);
        }

        [Fact]
        public async Task ValidComplexPermissionString_ShouldNotReportDiagnostic()
        {
            // 测试复杂但有效的权限字符串结构
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("PermissionTests/ValidComplexPermissionString.cs");
        }

        [Fact]
        public async Task NonAppPermissionClass_ShouldNotAnalyze()
        {
            // 测试非 AppPermission 类不应该被分析
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("PermissionTests/NonAppPermissionClass.cs");
        }

        [Fact]
        public async Task FieldPermissions_ShouldValidate()
        {
            // 测试字段声明中的权限字符串验证
            var expectedDiagnostics = new[]
            {
                // 检查字段声明（不允许）和格式错误
                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("InvalidQueryField"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("query_invalidField"),
                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("InvalidField"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("invalid")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/FieldPermissions.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task BasicPermissionValidation_ShouldWork()
        {
            // 基本的权限验证测试 - 验证测试框架是否正常工作
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("PermissionTests/BasicPermissionValidation.cs");
        }

        [Fact]
        public async Task PropertyPermissions_ShouldValidate()
        {
            // 测试属性初始化器中的权限字符串验证
            var expected = DiagnosticResultBuilder.Create("GEEX004")
                .WithArguments("invalid");

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/PropertyPermissions.cs", expected);
        }

        [Fact]
        public async Task MultipleInvalidPermissions_ShouldReportMultipleDiagnostics()
        {
            // 测试多个无效权限字符串应该报告多个诊断
            var expectedDiagnostics = new[]
            {
                // 构造函数中的无效权限 - 只有两段
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("invalid_permission"),

                // InvalidPermissions 类中的多个无效权限
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("invalid1"),

                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("too_many_parts_here_invalid"),

                // 字段中的无效权限
                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("InvalidField"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("invalidField")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/MultipleInvalidPermissions.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task PermissionShouldBePropertyNotField_ShouldValidate()
        {
            // 专门测试权限字符串必须是属性而非字段的规则
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("InvalidQueryField"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("query_invalidField"),

                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("InvalidField"),
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("invalid")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("PermissionTests/FieldPermissions.cs", expectedDiagnostics);
        }
    }
}
