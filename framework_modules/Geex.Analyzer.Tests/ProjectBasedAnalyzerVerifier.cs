using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Analyzer.Analyzer;
using Geex.Analyzer.TestCode.QueryTests;
using Geex.Validation;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

using MongoDB.Entities;

namespace Geex.Analyzer.Tests
{
    /// <summary>
    /// 基于项目引用的分析器验证器
    /// 用于测试分析器对真实项目代码的分析结果
    /// </summary>
    public static class ProjectBasedAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <summary>
        /// 验证分析器在指定源文件中的诊断结果
        /// </summary>
        /// <param name="sourceFileName">源文件名（相对于 TestCode 项目）</param>
        /// <param name="expectedDiagnostics">期望的诊断结果</param>
        public static async Task VerifyAnalyzerAsync(
            string sourceFileName,
            params DiagnosticResult[] expectedDiagnostics)
        {
            var test = new ProjectBasedAnalyzerTest<TAnalyzer>
            {
                TestCode = await GetSourceCodeAsync(sourceFileName),
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
                TestState =
                {
                    AdditionalReferences =
                    {

                        //// 添加基本的.NET Core引用
                        MetadataReference.CreateFromFile(typeof(ValidateRule).Assembly.Location), // System.Runtime
                        MetadataReference.CreateFromFile(typeof(TestEntity).Assembly.Location), // System.Runtime
                        MetadataReference.CreateFromFile(typeof(EntityBase<>).Assembly.Location), // System.Runtime
                        //MetadataReference.CreateFromFile(typeof(System.Linq.Queryable).Assembly.Location), // System.Linq.Queryable
                        //MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location), // System.Collections
                        //MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location), // System.ComponentModel.Annotations
                        //MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location), // System.Threading.Tasks
                        //MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location), // System.Linq
                    }
                }
            };

            // 添加期望的诊断结果
            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await test.RunAsync(CancellationToken.None);
        }


        /// <summary>
        /// 验证分析器在指定类型中的诊断结果
        /// </summary>
        /// <typeparam name="TTestClass">测试类型</typeparam>
        /// <param name="expectedDiagnostics">期望的诊断结果</param>
        public static async Task VerifyAnalyzerAsync<TTestClass>(
            params DiagnosticResult[] expectedDiagnostics)
        {
            var sourceFileName = GetSourceFileNameForType<TTestClass>();
            await VerifyAnalyzerAsync(sourceFileName, expectedDiagnostics);
        }

        /// <summary>
        /// 验证分析器在指定命名空间的所有类型中的诊断结果
        /// </summary>
        /// <param name="namespaceName">命名空间名称</param>
        /// <param name="expectedDiagnostics">期望的诊断结果</param>
        public static async Task VerifyNamespaceAsync(
            string namespaceName,
            params DiagnosticResult[] expectedDiagnostics)
        {
            var sourceFiles = GetSourceFilesForNamespace(namespaceName);

            foreach (var sourceFile in sourceFiles)
            {
                await VerifyAnalyzerAsync(sourceFile, expectedDiagnostics);
            }
        }

        /// <summary>
        /// 验证分析器不会在指定源文件中产生任何诊断
        /// </summary>
        /// <param name="sourceFileName">源文件名</param>
        public static async Task VerifyNoAnalyzerDiagnosticsAsync(string sourceFileName)
        {
            await VerifyAnalyzerAsync(sourceFileName);
        }

        /// <summary>
        /// 验证分析器不会在指定类型中产生任何诊断
        /// </summary>
        /// <typeparam name="TTestClass">测试类型</typeparam>
        public static async Task VerifyNoAnalyzerDiagnosticsAsync<TTestClass>()
        {
            await VerifyAnalyzerAsync<TTestClass>();
        }

        private static async Task<string> GetSourceCodeAsync(string sourceFileName)
        {
            var testCodeProjectPath = GetTestCodeProjectPath();
            var sourceFilePath = Path.Combine(testCodeProjectPath, sourceFileName);

            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException($"测试源文件不存在: {sourceFilePath}");
            }

            return await File.ReadAllTextAsync(sourceFilePath);
        }

        private static string GetSourceFileNameForType<TTestClass>()
        {
            var typeName = typeof(TTestClass).Name;
            var namespaceName = typeof(TTestClass).Namespace;

            // 根据命名空间确定文件名
            return namespaceName switch
            {
                var ns when ns.Contains("MediatR") => "MediatRTestCases.cs",
                var ns when ns.Contains("Permission") => "PermissionTestCases.cs",
                var ns when ns.Contains("Validation") => "ValidateAttributeTestCases.cs",
                var ns when ns.Contains("Query") => "QueryTestCases.cs",
                _ => throw new ArgumentException($"无法确定类型 {typeName} 的源文件")
            };
        }

        private static IEnumerable<string> GetSourceFilesForNamespace(string namespaceName)
        {
            return namespaceName switch
            {
                var ns when ns.Contains("MediatR") => new[] { "MediatRTestCases.cs" },
                var ns when ns.Contains("Permission") => new[] { "PermissionTestCases.cs" },
                var ns when ns.Contains("Validation") => new[] { "ValidateAttributeTestCases.cs" },
                var ns when ns.Contains("Query") => new[] { "QueryTestCases.cs" },
                _ => throw new ArgumentException($"无法确定命名空间 {namespaceName} 的源文件")
            };
        }

        private static string GetTestCodeProjectPath()
        {
            // 获取当前程序集目录
            var currentDirectory = Directory.GetCurrentDirectory();

            // 向上查找直到找到解决方案根目录
            var directory = new DirectoryInfo(currentDirectory);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("无法找到解决方案根目录");
            }

            return Path.Combine(directory.FullName, "Geex.Analyzer.TestCode");
        }
    }

    /// <summary>
    /// 基于项目的分析器测试类
    /// </summary>
    internal class ProjectBasedAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, GeexOnlyVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public ProjectBasedAnalyzerTest()
        {
            // 设置 C# 语言版本
            SolutionTransforms.Add((solution, projectId) =>
            {
                var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                var parseOptions = solution.GetProject(projectId).ParseOptions as CSharpParseOptions;

                if (parseOptions != null)
                {
                    parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.Latest);
                    solution = solution.WithProjectParseOptions(projectId, parseOptions);
                }

                return solution;
            });

            // 配置DiagnosticTest不要验证未指定的诊断属性
            TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = base.CreateCompilationOptions();

            // 设置编译选项以忽略某些警告（因为测试代码可能包含故意的错误）
            return compilationOptions.WithSpecificDiagnosticOptions(
                compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
        }

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = { "/warnaserror:nullable" };
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

            // 添加额外需要忽略的诊断
            var additionalDiagnostics = new Dictionary<string, ReportDiagnostic>
            {
                // 忽略编译错误（测试代码可能包含故意的错误）
                ["CS0246"] = ReportDiagnostic.Suppress, // 找不到类型或命名空间名称
                ["CS0103"] = ReportDiagnostic.Suppress, // 当前上下文中不存在名称
                ["CS0117"] = ReportDiagnostic.Suppress, // 不包含...的定义
                ["CS0234"] = ReportDiagnostic.Suppress, // 命名空间中不存在类型或命名空间名称
                ["CS0305"] = ReportDiagnostic.Suppress, // 使用泛型类型或方法需要参数
                ["CS0308"] = ReportDiagnostic.Suppress, // 非泛型类型或方法不能与类型参数一起使用
                ["CS0311"] = ReportDiagnostic.Suppress, // 类型不能用作泛型类型或方法中的类型参数
                ["CS0518"] = ReportDiagnostic.Suppress, // 未定义或导入预定义类型
                ["CS0535"] = ReportDiagnostic.Suppress, // 没有实现接口成员
                ["CS0619"] = ReportDiagnostic.Suppress, // 成员已过时
                ["CS0620"] = ReportDiagnostic.Suppress, // 不能在字段初始值设定项中使用索引
                ["CS0649"] = ReportDiagnostic.Suppress, // 字段从未赋值，将始终具有默认值
                ["CS0660"] = ReportDiagnostic.Suppress, // 类型定义运算符 == 或运算符 !=，但不重写Object.Equals
                ["CS0661"] = ReportDiagnostic.Suppress, // 类型定义运算符 == 或运算符 !=，但不重写Object.GetHashCode
                ["CS0693"] = ReportDiagnostic.Suppress, // 类型参数与外部类型的类型参数具有相同的名称
                ["CS1061"] = ReportDiagnostic.Suppress, // 不包含...的定义，并且找不到可接受第一个...类型参数的可访问扩展方法
                ["CS1503"] = ReportDiagnostic.Suppress, // 参数类型不匹配
                ["CS1729"] = ReportDiagnostic.Suppress, // 不包含带有参数的构造函数
                ["CS7036"] = ReportDiagnostic.Suppress, // 没有给出与所需正式参数对应的参数
            };

            return nullableWarnings.AddRange(additionalDiagnostics);
        }
    }

    /// <summary>
    /// 诊断结果构建器，用于简化诊断结果的创建
    /// </summary>
    public static class DiagnosticResultBuilder
    {
        /// <summary>
        /// 创建指定 ID 的诊断结果
        /// </summary>
        /// <param name="diagnosticId">诊断 ID</param>
        /// <returns>诊断结果</returns>
        public static DiagnosticResult Create(string diagnosticId)
        {
            return new DiagnosticResult(diagnosticId, DiagnosticSeverity.Warning);
        }

        /// <summary>
        /// 创建指定 ID 和参数的诊断结果
        /// </summary>
        /// <param name="diagnosticId">诊断 ID</param>
        /// <param name="arguments">消息参数</param>
        /// <returns>诊断结果</returns>
        public static DiagnosticResult Create(string diagnosticId, params object[] arguments)
        {
            return new DiagnosticResult(diagnosticId, DiagnosticSeverity.Warning).WithArguments(arguments);
        }

        /// <summary>
        /// 创建指定 ID、位置和参数的诊断结果
        /// </summary>
        /// <param name="diagnosticId">诊断 ID</param>
        /// <param name="line">行号</param>
        /// <param name="column">列号</param>
        /// <param name="arguments">消息参数</param>
        /// <returns>诊断结果</returns>
        public static DiagnosticResult Create(string diagnosticId, int line, int column, params object[] arguments)
        {
            return Create(diagnosticId, arguments).WithLocation(line, column);
        }

        /// <summary>
        /// 创建指定 ID、位置范围和参数的诊断结果
        /// </summary>
        /// <param name="diagnosticId">诊断 ID</param>
        /// <param name="startLine">起始行号</param>
        /// <param name="startColumn">起始列号</param>
        /// <param name="endLine">结束行号</param>
        /// <param name="endColumn">结束列号</param>
        /// <param name="arguments">消息参数</param>
        /// <returns>诊断结果</returns>
        public static DiagnosticResult Create(string diagnosticId, int startLine, int startColumn, int endLine, int endColumn, params object[] arguments)
        {
            return Create(diagnosticId, arguments).WithSpan(startLine, startColumn, endLine, endColumn);
        }
    }
}
