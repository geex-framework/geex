using System.Threading.Tasks;
using Geex.Analyzer.Analyzer;
using Geex.Analyzer.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Geex.Analyzer.Tests
{
    using AnalyzerVerifier = CSharpAnalyzerVerifier<PermissionStringAnalyzer, GeexOnlyVerifier>;

    public class PermissionStringAnalyzerTests
    {
        [Fact]
        public async Task ValidPermissionString_ShouldNotReportDiagnostic()
        {
            var test = """
                       namespace Test
                       {
                           public abstract class AppPermission<T> : AppPermission
                           {
                               protected AppPermission(string value) : base(value) { }
                           }
                           
                           public class AppPermission
                           {
                               protected AppPermission(string value) { }
                           }
                           
                           public class TestPermission : AppPermission<TestPermission>
                           {
                               public const string Prefix = "test";
                               
                               public TestPermission(string value) : base($"{Prefix}_{value}")
                               {
                               }
                               
                               public static TestPermission Create { get; } = new("mutation_createTest");
                               public static TestPermission Edit { get; } = new("mutation_editTest");
                               public static TestPermission Query { get; } = new("query_tests");
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task InvalidPermissionString_ShouldReportDiagnostic()
        {
            var test = """
                       namespace Test
                       {
                           public abstract class AppPermission<T> : AppPermission
                           {
                               protected AppPermission(string value) : base(value) { }
                           }
                           
                           public class AppPermission
                           {
                               protected AppPermission(string value) { }
                           }
                           
                           public class TestPermission : AppPermission<TestPermission>
                           {
                               public TestPermission(string value) : base(value)
                               {
                               }
                               
                               public static TestPermission Invalid1 { get; } = new("invalid");
                           }
                       }
                       """;

            var expected1 = AnalyzerVerifier.Diagnostic("GEEX004")
                .WithSpan(19, 75, 19, 84)
                .WithArguments("invalid");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected1);
        }

        [Fact]
        public async Task PermissionStringInConstructor_ShouldValidate()
        {
            var test = """
                       namespace Test
                       {
                           public abstract class AppPermission<T> : AppPermission
                           {
                               protected AppPermission(string value) : base(value) { }
                           }
                           
                           public class AppPermission
                           {
                               protected AppPermission(string value) { }
                           }
                           
                           public class TestPermission : AppPermission<TestPermission>
                           {
                               public TestPermission(string value) : base("invalid_permission")
                               {
                               }
                           }
                       }
                       """;

            var expected = AnalyzerVerifier.Diagnostic("GEEX004")
                .WithSpan(109, 64, 109, 82)
                .WithArguments("invalid_permission");

            await AnalyzerVerifier.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task ValidComplexPermissionString_ShouldNotReportDiagnostic()
        {
            var test = """
                       namespace Test
                       {
                           public abstract class AppPermission<T> : AppPermission
                           {
                               protected AppPermission(string value) : base(value) { }
                           }
                           
                           public class AppPermission
                           {
                               protected AppPermission(string value) { }
                           }
                           
                           public class IdentityPermission : AppPermission<IdentityPermission>
                           {
                               public const string Prefix = "identity";
                               
                               public IdentityPermission(string value) : base($"{Prefix}_{value}")
                               {
                               }
                               
                               public class UserPermission : IdentityPermission
                               {
                                   public static UserPermission Query { get; } = new("query_users");
                                   public static UserPermission Create { get; } = new("mutation_createUser");
                                   public static UserPermission Edit { get; } = new("mutation_editUser");
                                   
                                   public UserPermission(string value) : base(value)
                                   {
                                   }
                               }
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NonAppPermissionClass_ShouldNotAnalyze()
        {
            var test = """
                       namespace Test
                       {
                           public class RegularClass
                           {
                               public static string InvalidPermission { get; } = "invalid";
                               
                               public RegularClass(string value)
                               {
                               }
                           }
                       }
                       """;

            await AnalyzerVerifier.VerifyAnalyzerAsync(test);
        }
    }
}
