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
        public async Task UnsupportedStringLength_InWhere_ShouldReportDiagnostic()
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
                               var result = list.AsQueryable().Where(x => x.Name.Length > 5);
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX004")
                .WithSpan(18, 54, 18, 65)
                .WithArguments("System.String.Length", "在$match阶段不支持String.Length，考虑使用聚合管道");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
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
                .WithArguments("System.DateTime.Ticks", "在$match阶段不支持此DateTime属性，考虑使用支持的日期操作");

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
                .WithArguments("System.DateTime.TimeOfDay", "在$match阶段不支持此DateTime属性，考虑使用支持的日期操作");

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
    }
}
