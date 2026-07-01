using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Geex.Analyzer.Tests
{
    /// <summary>
    /// 基于 <see cref="DefaultVerifier"/>，当期望的 <see cref="DiagnosticResult"/> 未指定位置时，
    /// 不强制要求实际诊断也为 <see cref="Location.None"/>（兼容既有只断言 Id/参数的测试写法）。
    /// </summary>
    public class GeexVerifier : DefaultVerifier
    {
        public GeexVerifier()
        {
        }

        private GeexVerifier(ImmutableStack<string> context)
            : base(context)
        {
        }

        public override IVerifier PushContext(string context)
        {
            return new GeexVerifier(Context.Push(context));
        }

        public override void Equal<T>(T expected, T actual, string message = null)
        {
            if (expected is Location expectedLocation &&
                actual is Location actualLocation &&
                expectedLocation == Location.None &&
                actualLocation != Location.None)
            {
                return;
            }

            base.Equal(expected, actual, message);
        }
    }
}
