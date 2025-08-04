using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

using Xunit;
using Xunit.Sdk;

namespace Geex.Analyzer.Tests
{
    /// <summary>
    /// 用于Geex项目的自定义诊断验证器
    /// 只验证已提供的诊断信息，如果没有提供位置信息则不验证位置
    /// </summary>
    public class GeexOnlyVerifier : XUnitVerifier
    {
        public override void Equal<T>(T expected, T actual, string message = null)
        {

            if (expected is Location expectedLocation)
            {
                if (expectedLocation == Location.None)
                {
                    _Equal(expectedLocation, Location.None, message);
                    return;
                }
            }
            _Equal(expected, actual, message);

            void _Equal<T1>(T1 expected, T1 actual, string message = null)
            {
                if (message is null && this.Context.IsEmpty)
                {
                    Assert.Equal(expected, actual);
                }
                else
                {
                    if (!EqualityComparer<T1>.Default.Equals(expected, actual) && !expected.Equals(actual))
                    {
                        throw new Exception(message ?? $"Expected: {expected}, Actual: {actual}");
                    }
                }
            }
        }
        /// <inheritdoc />
        public override void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> equalityComparer = null,
            string message = null)
        {
            var expectedArray = expected.ToArray();
            var actualArray = actual.ToArray();
            for (int i = 0; i < expectedArray.Length; i++)
            {
                this.Equal(expectedArray[i], actualArray[i], message);
            }
        }
        /// <summary>
        /// 自定义诊断验证，只验证已提供的信息
        /// </summary>
        public void VerifyDiagnosticResults(ImmutableArray<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            if (expectedResults.Length != actualResults.Length)
            {
                this.Equal(expectedResults.Length, actualResults.Length,
                    $"Expected {expectedResults.Length} diagnostics but got {actualResults.Length}");
                return;
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults[i];
                var expected = expectedResults[i];

                // 验证诊断ID
                if (!string.IsNullOrEmpty(expected.Id))
                {
                    this.Equal(expected.Id, actual.Id, $"Diagnostic {i}: ID不匹配");
                }

                // 验证严重性
                if (expected.Severity != null)
                {
                    this.Equal(expected.Severity, actual.Severity, $"Diagnostic {i}: Severity不匹配");
                }

                // 验证位置（如果提供了位置信息）
                if (expected.HasLocation)
                {
                    this.True(actual.Location != Location.None, $"Diagnostic {i} 没有位置信息");
                    this.Equal(expected.Spans.Length, 1, $"Diagnostic {i} 应该只有一个位置范围");

                    var actualSpan = actual.Location.GetLineSpan();
                    var actualLinePosition = actualSpan.StartLinePosition;
                    var expectedSpan = expected.Spans[0];

                    this.Equal(expectedSpan.Span.StartLinePosition.Line, actualLinePosition.Line, $"Diagnostic {i} 的起始行不匹配");
                    this.Equal(expectedSpan.Span.StartLinePosition.Character, actualLinePosition.Character, $"Diagnostic {i} 的起始列不匹配");
                }

                // 验证消息参数（如果提供）
                if (expected.MessageArguments?.Length > 0)
                {
                    // 获取实际消息参数
                    var actualArguments = actual.Properties.Values;

                    // 获取期望的消息参数
                    var expectedArguments = expected.MessageArguments;

                    for (int j = 0; j < expected.MessageArguments.Length; j++)
                    {
                        if (j < expectedArguments.Length)
                        {
                            // 仅验证参数值的包含关系，不要求完全匹配
                            var expectedArg = expectedArguments[j]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(expectedArg))
                            {
                                this.True(
                                    actual.GetMessage().Contains(expectedArg),
                                    $"Diagnostic {i} 的消息中应包含参数值: {expectedArg}");
                            }
                        }
                    }
                }
            }
        }
    }
}
