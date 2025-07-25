using System.Threading.Tasks;

using Geex.Analyzer.Analyzer;
using Geex.Analyzer.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = CSharpAnalyzerVerifier<NotSupportedQueryAnalyzer, GeexOnlyVerifier>;

    public class NotSupportedQueryAnalyzerTests
    {
        [Fact]
        public async Task UnsupportedGetHashCode_InWhere_ShouldReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Name.GetHashCode() > 100);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(18, 58, 18, 79)
                .WithArguments("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task UnsupportedToString_InSelect_ShouldReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public int Age { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Select(x => x.Age.ToString());
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(18, 57, 18, 72)
                .WithArguments("ToString", "在查询中避免使用ToString，建议在查询结果上调用");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SupportedStringLength_InSelect_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Select(x => x.Name.Length);
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task UnsupportedDateTimeTicks_InOrderBy_ShouldReportDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTime CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().OrderBy(x => x.CreatedDate.Ticks);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX004")
                .WithSpan(19, 58, 19, 77)
                .WithArguments("System.DateTime.Ticks", "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task UnsupportedDateTimeProperty_InWhere_ShouldReportSpecialDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTime CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.CreatedDate.Year > 2020);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX005")
                .WithSpan(18, 54, 18, 72)
                .WithArguments("System.DateTime.Year");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SupportedDateTimeProperty_InSelect_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTime CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Select(x => x.CreatedDate.Year);
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task UnsupportedDateTimeOffsetProperty_InWhere_ShouldReportSpecialDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTimeOffset CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.CreatedDate.Offset.Hours > 0);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX005")
                .WithSpan(19, 52, 19, 72)
                .WithArguments("System.DateTimeOffset.Offset");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task UnsupportedEquals_InWhere_ShouldReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Name.Equals("test"));
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(18, 54, 18, 74)
                .WithArguments("Equals", "对于字段比较，请使用==运算符代替Equals方法");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SupportedEqualsOperator_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Name == "test");
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NonEntityBaseType_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using System.Collections.Generic;

                       class RegularClass
                       {
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<RegularClass>();
                               var result = list.AsQueryable().Where(x => x.Name.GetHashCode() > 100);
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task MethodCallOutsideQuery_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var results = list.AsQueryable().ToList();
                               var hashCodes = results.Select(x => x.Name.GetHashCode()).ToList();
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task UnsupportedGetType_InGroupBy_ShouldReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Category { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().GroupBy(x => x.Category.GetType());
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(18, 60, 18, 80)
                .WithArguments("GetType", "GetType方法在MongoDB查询中不受支持");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task UnsupportedDateTimeTimeOfDay_InSelect_ShouldReportDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTime CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Select(x => x.CreatedDate.TimeOfDay);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX004")
                .WithSpan(19, 57, 19, 80)
                .WithArguments("System.DateTime.TimeOfDay", "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task UnsupportedStringMethod_WithNonConstantParameter_InWhere_ShouldReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                           public string SearchTerm { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Name.StartsWith(x.SearchTerm));
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(19, 54, 19, 82)
                .WithArguments("string.StartsWith", "在Where子句中，StartsWith只支持常量字符串参数");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SupportedStringMethod_WithConstantParameter_InWhere_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Name.StartsWith("test"));
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task UnsupportedTimeSpanProperty_InWhere_ShouldReportDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public TimeSpan Duration { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Duration.Days > 1);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX004")
                .WithSpan(18, 54, 18, 69)
                .WithArguments("System.TimeSpan.Days", "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SupportedTimeSpanProperty_InSelect_ShouldNotReportDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public TimeSpan Duration { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Select(x => x.Duration.TotalHours);
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task UnsupportedDateTimeKind_InWhere_ShouldReportSpecialDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTime CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.CreatedDate.Kind == DateTimeKind.Utc);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX005")
                .WithSpan(18, 54, 18, 72)
                .WithArguments("System.DateTime.Kind");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task MultipleUnsupportedMethods_ShouldReportMultipleDiagnostics()
        {
            var test = """
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public string Name { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.Name.GetHashCode() > 100 && x.Name.ToString().Length > 5);
                           }
                       }
                       """;

            var expected1 = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(18, 58, 18, 79)
                .WithArguments("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持");

            var expected2 = AnalyzerVerifier.Diagnostic("GEEX003")
                .WithSpan(18, 89, 18, 104)
                .WithArguments("ToString", "在查询中避免使用ToString，建议在查询结果上调用");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [Fact]
        public async Task UnsupportedDateTimeOffsetOffset_InWhere_ShouldReportSpecialDiagnostic()
        {
            var test = """
                       using System;
                       using System.Linq;
                       using MongoDB.Entities;
                       using System.Collections.Generic;

                       namespace MongoDB.Entities { public interface IEntityBase { string Id { get; set; } } }

                       class TestEntity : IEntityBase
                       {
                           public string Id { get; set; }
                           public DateTimeOffset CreatedDate { get; set; }
                       }

                       class TestClass
                       {
                           void Method()
                           {
                               var list = new List<TestEntity>();
                               var result = list.AsQueryable().Where(x => x.CreatedDate.Offset.TotalHours > 0);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX005")
                .WithSpan(19, 52, 19, 72)
                .WithArguments("System.DateTimeOffset.Offset");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }
    }
}
