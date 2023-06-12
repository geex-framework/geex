using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

using Geex.Common.Abstractions;
using HotChocolate.Execution.Options;
using ImpromptuInterface;

namespace Geex.Common.Logging
{
    public class LoggingModuleOptions : IGeexModuleOption<LoggingModule>
    {
        public TracingPreference TracingPreference { get; set; } = TracingPreference.OnDemand;
        public GeexApmConfig ElasticApm { get; set; }
    }

    public class GeexApmConfig : IConfigurationReader
    {
        public string ApiKey { get; }
        public IReadOnlyCollection<string> ApplicationNamespaces { get; }
        public string CaptureBody { get; }
        public List<string> CaptureBodyContentTypes { get; }
        public bool CaptureHeaders { get; }
        public bool CentralConfig { get; }
        public string CloudProvider { get; }
        public IReadOnlyList<WildcardMatcher> DisableMetrics { get; }
        public bool Enabled { get; }
        public string Environment { get; }
        public IReadOnlyCollection<string> ExcludedNamespaces { get; }
        public double ExitSpanMinDuration { get; }
        public TimeSpan FlushInterval { get; }
        public IReadOnlyDictionary<string, string> GlobalLabels { get; }
        public string HostName { get; }
        public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get; }
        public LogLevel LogLevel { get; }
        public int MaxBatchEventCount { get; }
        public int MaxQueueEventCount { get; }
        public double MetricsIntervalInMilliseconds { get; }
        public bool Recording { get; }
        public IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; }
        public string SecretToken { get; }
        public string ServerCert { get; }
        public Uri ServerUrl { get; }
        public IReadOnlyList<Uri> ServerUrls { get; }

        /// <inheritdoc />
        public bool UseWindowsCredentials { get; }
        public string ServiceName { get; }
        public string ServiceNodeName { get; }
        public string ServiceVersion { get; }
        public bool SpanCompressionEnabled { get; }
        public double SpanCompressionExactMatchMaxDuration { get; }
        public double SpanCompressionSameKindMaxDuration { get; }

        /// <inheritdoc />
        public double SpanStackTraceMinDurationInMilliseconds { get; }
        public double SpanFramesMinDurationInMilliseconds { get; }
        public int StackTraceLimit { get; }
        public bool TraceContextIgnoreSampledFalse { get; }

        /// <inheritdoc />
        public string TraceContinuationStrategy { get; }
        public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get; }
        public int TransactionMaxSpans { get; }
        public double TransactionSampleRate { get; }
        public bool UseElasticTraceparentHeader { get; }
        public bool VerifyServerCert { get; }
        public bool EnableOpenTelemetryBridge { get; }
    }
}
