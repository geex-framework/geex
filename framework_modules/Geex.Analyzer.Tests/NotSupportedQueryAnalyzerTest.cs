using System.Threading.Tasks;

using Geex.Analyzer.Analyzer;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = ProjectBasedAnalyzerVerifier<NotSupportedQueryAnalyzer>;

    /// <summary>
    /// NotSupportedQueryAnalyzer 的测试类
    /// 使用 Geex.Analyzer.TestCode 项目中的真实代码进行测试
    /// </summary>
    public class NotSupportedQueryAnalyzerTests
    {
        [Fact]
        public async Task UnsupportedGetHashCode_InWhere_ShouldReportDiagnostic()
        {
            // 测试在 Where 子句中使用不支持的 GetHashCode 方法
            var expected = DiagnosticResultBuilder.Create("GEEX003")
                .WithArguments("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持");

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedGetHashCodeInWhere.cs", expected);
        }

        [Fact]
        public async Task UnsupportedToString_InSelect_ShouldReportDiagnostic()
        {
            // 测试在 Select 子句中使用不支持的 ToString 方法
            var expected = DiagnosticResultBuilder.Create("GEEX003")
                .WithArguments("ToString", "在查询中避免使用ToString，建议在查询结果上调用");

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedToStringInSelect.cs", expected);
        }

        [Fact]
        public async Task SupportedStringLength_InSelect_ShouldNotReportDiagnostic()
        {
            // 测试在 Select 子句中使用支持的 String.Length 属性
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/SupportedStringLength.cs");
        }

        [Fact]
        public async Task UnsupportedDateTimeTicks_InOrderBy_ShouldReportDiagnostic()
        {
            // 测试在 OrderBy 子句中使用不支持的 DateTime.Ticks 属性
            var expected = DiagnosticResultBuilder.Create("GEEX004")
                .WithArguments("System.DateTime.Ticks", "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等");

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedDateTimeTicks.cs", expected);
        }

        [Fact]
        public async Task UnsupportedDateTimeProperty_InWhere_ShouldReportSpecialDiagnostic()
        {
            // 测试在 Where 子句中使用 DateTime 属性应该报告特殊诊断 GEEX005
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("System.DateTime.Year"),

                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("System.DateTime.Month"),

                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("System.DateTime.Day"),

                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("System.DateTime.Kind")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedDateTimePropertyInWhere.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task SupportedDateTimeProperty_InSelect_ShouldNotReportDiagnostic()
        {
            // 测试在 Select 子句中使用支持的 DateTime 属性
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/SupportedDateTimeProperty.cs");
        }

        [Fact]
        public async Task UnsupportedDateTimeOffsetProperty_InWhere_ShouldReportSpecialDiagnostic()
        {
            // 测试在 Where 子句中使用 DateTimeOffset 属性应该报告特殊诊断
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("System.DateTimeOffset.Offset"),

                DiagnosticResultBuilder.Create("GEEX005")
                    .WithArguments("System.DateTimeOffset.Offset")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedDateTimeOffsetProperty.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task UnsupportedEquals_InWhere_ShouldReportDiagnostic()
        {
            // 测试在 Where 子句中使用 Equals 方法
            var expected = DiagnosticResultBuilder.Create("GEEX003")
                .WithArguments("Equals", "对于字段比较，请使用==运算符代替Equals方法");

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedEqualsInWhere.cs", expected);
        }

        [Fact]
        public async Task SupportedEqualsOperator_ShouldNotReportDiagnostic()
        {
            // 测试使用 == 运算符不应该报告诊断
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/SupportedEqualsOperator.cs");
        }

        [Fact]
        public async Task NonEntityBaseType_ShouldNotReportDiagnostic()
        {
            // 测试非实体类型的查询不应该报告诊断
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/NonEntityBaseType.cs");
        }

        [Fact]
        public async Task MethodCallOutsideQuery_ShouldNotReportDiagnostic()
        {
            // 测试查询外的方法调用不应该报告诊断
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/MethodCallOutsideQuery.cs");
        }

        [Fact]
        public async Task UnsupportedGetType_InGroupBy_ShouldReportDiagnostic()
        {
            // 测试在 GroupBy 子句中使用 GetType 方法
            var expected = DiagnosticResultBuilder.Create("GEEX003")
                .WithArguments("GetType", "GetType方法在MongoDB查询中不受支持");

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedGetTypeInGroupBy.cs", expected);
        }

        [Fact]
        public async Task UnsupportedDateTimeTimeOfDay_InSelect_ShouldReportDiagnostic()
        {
            // 测试在 Select 子句中使用不支持的 DateTime.TimeOfDay 属性
            var expected = DiagnosticResultBuilder.Create("GEEX004")
                .WithArguments("System.DateTime.TimeOfDay", "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等");

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedDateTimeTimeOfDay.cs", expected);
        }

        [Fact]
        public async Task UnsupportedStringMethod_WithNonConstantParameter_InWhere_ShouldReportDiagnostic()
        {
            // 测试在 Where 子句中使用非常量参数的字符串方法
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("string.StartsWith", "在Where子句中，StartsWith只支持常量字符串参数"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("string.EndsWith", "在Where子句中，EndsWith只支持常量字符串参数"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("string.Contains", "在Where子句中，Contains只支持常量字符串参数")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedStringMethodWithNonConstant.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task SupportedStringMethod_WithConstantParameter_InWhere_ShouldNotReportDiagnostic()
        {
            // 测试在 Where 子句中使用常量参数的字符串方法
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/SupportedStringMethodWithConstant.cs");
        }

        [Fact]
        public async Task UnsupportedTimeSpanProperty_InWhere_ShouldReportDiagnostic()
        {
            // 测试在 Where 子句中使用不支持的 TimeSpan 属性
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("System.TimeSpan.Days", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性"),

                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("System.TimeSpan.Hours", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性"),

                DiagnosticResultBuilder.Create("GEEX004")
                    .WithArguments("System.TimeSpan.Minutes", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/UnsupportedTimeSpanProperty.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task SupportedTimeSpanProperty_InSelect_ShouldNotReportDiagnostic()
        {
            // 测试在 Select 子句中使用支持的 TimeSpan 属性
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/SupportedTimeSpanProperty.cs");
        }

        [Fact]
        public async Task MultipleUnsupportedMethods_ShouldReportMultipleDiagnostics()
        {
            // 测试多个不支持的方法应该报告多个诊断
            var expectedDiagnostics = new[]
            {
                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持"),

                DiagnosticResultBuilder.Create("GEEX003")
                    .WithArguments("ToString", "在查询中避免使用ToString，建议在查询结果上调用")
            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/MultipleUnsupportedMethods.cs", expectedDiagnostics);
        }

        [Fact(Skip = "todo")]
        public async Task AllUnsupportedMethods_ShouldReportComprehensiveDiagnostics()
        {
            // 测试所有不支持的方法和属性应该报告全面的诊断
            var expectedDiagnostics = new[]
            {
                // 不支持的方法 (GEEX003)
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("GetType", "GetType方法在MongoDB查询中不受支持"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("Equals", "对于字段比较，请使用==运算符代替Equals方法"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("string.StartsWith", "在Where子句中，StartsWith只支持常量字符串参数"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("string.EndsWith", "在Where子句中，EndsWith只支持常量字符串参数"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("string.Contains", "在Where子句中，Contains只支持常量字符串参数"),

                                // DateTime/DateTimeOffset 属性在 $match 阶段 (GEEX005)
                DiagnosticResultBuilder.Create("GEEX005").WithArguments("System.DateTime.Year"),
                DiagnosticResultBuilder.Create("GEEX005").WithArguments("System.DateTime.Month"),
                DiagnosticResultBuilder.Create("GEEX005").WithArguments("System.DateTime.Day"),
                DiagnosticResultBuilder.Create("GEEX005").WithArguments("System.DateTime.Kind"),
                DiagnosticResultBuilder.Create("GEEX005").WithArguments("System.DateTimeOffset.Offset"),
                DiagnosticResultBuilder.Create("GEEX005").WithArguments("System.DateTimeOffset.Offset"),

                // 不支持的属性 (GEEX004)
                DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.DateTime.Ticks", "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等"),
                DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.DateTimeOffset.UtcDateTime", "在$match阶段不支持此DateTimeOffset属性，考虑使用聚合管道"),
                DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.DateTimeOffset.LocalDateTime", "在$match阶段不支持此DateTimeOffset属性，考虑使用聚合管道"),
                DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.DateTime.TimeOfDay", "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等"),
                //DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.TimeSpan.Days", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性"),
                //DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.TimeSpan.Hours", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性"),
                //DiagnosticResultBuilder.Create("GEEX004").WithArguments("System.TimeSpan.Minutes", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("ToString", "在查询中避免使用ToString，建议在查询结果上调用"),
                DiagnosticResultBuilder.Create("GEEX003").WithArguments("ReferenceEquals", "ReferenceEquals在MongoDB查询中不受支持"),

            };

            await AnalyzerVerifier.VerifyAnalyzerAsync("QueryTests/AllUnsupportedMethods.cs", expectedDiagnostics);
        }

        [Fact]
        public async Task ComplexQueryExpressions_ShouldHandleCorrectly()
        {
            // 测试复杂查询表达式应该正确处理
            await AnalyzerVerifier.VerifyNoAnalyzerDiagnosticsAsync("QueryTests/ComplexQueryExpressions.cs");
        }
    }
}
