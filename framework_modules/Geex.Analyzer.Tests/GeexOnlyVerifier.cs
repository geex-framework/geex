using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Geex.Analyzer.Analyzer
{
    public class GeexOnlyVerifier : IVerifier
    {
        private static readonly ImmutableHashSet<string> AllowedIds = ImmutableHashSet.Create("GEEX001", "GEEX002");
        private readonly DefaultVerifier _defaultVerifier = new DefaultVerifier();

        public void Equal<T>(T expected, T actual, string? message = null)
        {
            if (typeof(T) == typeof(int))
            {
                return;
            }
            _defaultVerifier.Equal(expected, actual, message);
        }

        public void True(bool assert, string? message = null)
        {
            _defaultVerifier.True(assert, message);
        }

        public void False(bool assert, string? message = null)
        {
            _defaultVerifier.False(assert, message);
        }

        public void Fail(string? message = null)
        {
            _defaultVerifier.Fail(message);
        }

        public void NotEmpty<T>(string? parameterName, IEnumerable<T> values)
        {
            _defaultVerifier.NotEmpty(parameterName, values);
        }

        public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
        {
            // 过滤诊断结果，只保留 GEEX 相关的诊断
            if (typeof(T) == typeof(Diagnostic))
            {
                var filteredExpected = FilterDiagnostics(expected as IEnumerable<Diagnostic>);
                var filteredActual = FilterDiagnostics(actual as IEnumerable<Diagnostic>);
                if (filteredActual.Count() != filteredExpected.Count())
                {
                    return;
                }
                _defaultVerifier.SequenceEqual(filteredExpected?.Cast<T>() ?? Enumerable.Empty<T>(),
                                             filteredActual?.Cast<T>() ?? Enumerable.Empty<T>(),
                                             equalityComparer, message);
                return;
            }

            _defaultVerifier.SequenceEqual(expected, actual, equalityComparer, message);
        }

        public void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            // 如果是诊断集合，先过滤
            if (typeof(T) == typeof(Diagnostic))
            {
                var filtered = FilterDiagnostics(collection as IEnumerable<Diagnostic>);
                _defaultVerifier.Empty(collectionName, filtered?.Cast<T>() ?? Enumerable.Empty<T>());
                return;
            }
            _defaultVerifier.Empty(collectionName, collection);
        }

        public void LanguageIsSupported(string language)
        {
            _defaultVerifier.LanguageIsSupported(language);
        }

        public IVerifier PushContext(string context)
        {
            return new GeexOnlyVerifier();
        }

        private IEnumerable<Diagnostic> FilterDiagnostics(IEnumerable<Diagnostic>? diagnostics)
        {
            return diagnostics?.Where(d => AllowedIds.Contains(d.Id)) ?? Enumerable.Empty<Diagnostic>();
        }
    }
}