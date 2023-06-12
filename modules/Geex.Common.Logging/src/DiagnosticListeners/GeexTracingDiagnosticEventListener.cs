using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Elastic.Apm;
using Elastic.Apm.Api;

using Geex.Common.Abstractions;
using Geex.Common.Logging;

using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geex.Common.Gql
{
    internal class GeexTracingDiagnosticEventListener : ExecutionDiagnosticEventListener
    {
        private const string _extensionKey = "tracing";
        private readonly ILogger<GeexTracingDiagnosticEventListener> logger;
        private TracingPreference _tracingPreference;
        private bool _apmEnabled;
        private readonly ITimestampProvider _timestampProvider;

        public GeexTracingDiagnosticEventListener(
          ILogger<GeexTracingDiagnosticEventListener> logger,
          LoggingModuleOptions options,
          ITimestampProvider? timestampProvider)
        {
            this.logger = logger;
            this._tracingPreference = options.TracingPreference;
            this._timestampProvider = timestampProvider;
        }

        public override bool EnableResolveFieldValue => true;

        /// <inheritdoc />
        public override IDisposable ExecuteOperation(IRequestContext context)
        {
            this._apmEnabled = Agent.IsConfigured;
            if (!this.IsEnabled(context.ContextData))
                return EmptyScope;
            DateTime startTime = this._timestampProvider.UtcNow();
            this.logger.LogInformationWithData(new EventId((nameof(GeexTracingOperationScope) + "Start").GetHashCode(), nameof(GeexTracingOperationScope) + "Start"), "Request started.", new { QueryId = context.Request.QueryId, Query = context.GetOperationDetails(), OperationName = context.Request.OperationName, Variables = context.Request.VariableValues?.ToDictionary<KeyValuePair<string, object>, string, object>(x => x.Key, x => (x.Value as IValueNode)?.Value) });
            GeexTracingResultBuilder builder = CreateBuilder(context, logger);
            return new GeexTracingOperationScope(context, logger, startTime, builder, this._timestampProvider);
        }

        public override IDisposable ParseDocument(IRequestContext context)
        {
            return new ParseDocumentScope(context, this._timestampProvider);
        }

        public override IDisposable ValidateDocument(IRequestContext context)
        {
            return new ValidateDocumentScope(context, this._timestampProvider);
        }

        public override IDisposable ResolveFieldValue(IMiddlewareContext context)
        {
            return new ResolveFieldValueScope(context, this._timestampProvider);
        }

        private static GeexTracingResultBuilder CreateBuilder(IRequestContext context,
            ILogger<GeexTracingDiagnosticEventListener> logger)
        {
            GeexTracingResultBuilder tracingResultBuilder = new GeexTracingResultBuilder(logger);
            context.ContextData["ApolloTracingResultBuilder"] = tracingResultBuilder;
            return tracingResultBuilder;
        }

        private static bool TryGetBuilder(
          IDictionary<string, object?> contextData,
          [NotNullWhen(true)] out GeexTracingResultBuilder? builder)
        {
            object obj;
            if (contextData.TryGetValue("ApolloTracingResultBuilder", out obj) && obj is GeexTracingResultBuilder tracingResultBuilder)
            {
                builder = tracingResultBuilder;
                return true;
            }
            builder = null;
            return false;
        }

        private bool IsEnabled(IDictionary<string, object?> contextData)
        {
            if (this._tracingPreference == TracingPreference.Always)
                return true;
            return this._tracingPreference == TracingPreference.OnDemand && contextData.ContainsKey("HotChocolate.Execution.EnableTracing");
        }

        private class ParseDocumentScope : IDisposable
        {
            private readonly GeexTracingResultBuilder? _builder;
            private readonly ITimestampProvider _timestampProvider;
            private readonly long _startTimestamp;
            private bool _disposed;
            private ISpan? _span;

            public ParseDocumentScope(
              IRequestContext context,
              ITimestampProvider timestampProvider)
            {
                this._builder = context.GetTypedContextData<GeexTracingResultBuilder>("ApolloTracingResultBuilder");
                this._timestampProvider = timestampProvider;
                this._startTimestamp = timestampProvider.NowInNanoseconds();
                if (Agent.IsConfigured)
                {
                    this._span = context.GetTypedContextData<ISpan>("ApmOperationSpan")?.StartSpan("parse_document", "request", "parse_document");
                }
            }

            public void Dispose()
            {
                if (this._disposed)
                    return;
                this._builder?.SetParsingResult(this._startTimestamp, this._timestampProvider.NowInNanoseconds());
                this._span?.End();
                this._disposed = true;
            }
        }

        private class ValidateDocumentScope : IDisposable
        {
            private readonly GeexTracingResultBuilder? _builder;
            private readonly ITimestampProvider _timestampProvider;
            private readonly long _startTimestamp;
            private bool _disposed;
            private ISpan? _span;

            public ValidateDocumentScope(
              IRequestContext context,
              ITimestampProvider timestampProvider)
            {
                this._builder = context.GetTypedContextData<GeexTracingResultBuilder>("ApolloTracingResultBuilder");
                this._timestampProvider = timestampProvider;
                this._startTimestamp = timestampProvider.NowInNanoseconds();
                if (Agent.IsConfigured)
                {
                    this._span = context.GetTypedContextData<ISpan>("ApmOperationSpan")?.StartSpan("validate_document", "request", "validate_document");
                }
            }

            public void Dispose()
            {
                if (this._disposed)
                    return;
                this._builder?.SetValidationResult(this._startTimestamp, this._timestampProvider.NowInNanoseconds());
                this._span?.End();
                this._disposed = true;
            }
        }

        private class ResolveFieldValueScope : IDisposable
        {
            private readonly IMiddlewareContext _context;
            private readonly GeexTracingResultBuilder? _builder;
            private readonly ITimestampProvider _timestampProvider;
            private readonly long _startTimestamp;
            private bool _disposed;
            private ISpan? _span;
            private ISpan? _parentSpan;

            public ResolveFieldValueScope(
              IMiddlewareContext context,
              ITimestampProvider timestampProvider)
            {
                this._context = context;
                this._builder = context.GetTypedContextData<GeexTracingResultBuilder>("ApolloTracingResultBuilder");

                this._timestampProvider = timestampProvider;
                this._startTimestamp = timestampProvider.NowInNanoseconds();
                if (Agent.IsConfigured)
                {
                    var pathStr = context.Path.Print();
                    var parentPath = context.Path.Parent;
                    while (parentPath != null && !context.ContextData.ContainsKey(parentPath?.Print() ?? ""))
                    {
                        parentPath = parentPath?.Parent;
                    }
                    var parentPathStr = parentPath?.Print();
                    this._parentSpan = context.GetTypedContextData<ISpan>(parentPathStr ?? "ApmOperationSpan");
                    this._span = this._parentSpan?.StartSpan(pathStr, "request", "field_resolve");
                    context.ContextData.Add(pathStr, this._span);
                }
            }

            public void Dispose()
            {
                if (this._disposed)
                    return;
                this._builder?.AddResolverResult(new GeexTracingResolverRecord(this._context, this._startTimestamp, this._timestampProvider.NowInNanoseconds()));
                this._span?.End();
                this._disposed = true;
            }
        }
    }

    internal class GeexTracingResultBuilder
    {
        private readonly ILogger<GeexTracingDiagnosticEventListener> logger;
        private const int _apolloTracingVersion = 1;
        private const long _ticksToNanosecondsMultiplicator = 100;
        private readonly ConcurrentQueue<GeexTracingResolverRecord> _resolverRecords = new ConcurrentQueue<GeexTracingResolverRecord>();
        private TimeSpan _duration;
        private ObjectResult? _parsingResult;
        private DateTimeOffset _startTime;
        private long _startTimestamp;
        private ObjectResult? _validationResult;
        private ISpan? _request_span;

        public GeexTracingResultBuilder(ILogger<GeexTracingDiagnosticEventListener> logger)
        {
            this.logger = logger;
        }

        public void SetOperationStartTime(DateTimeOffset startTime, long startTimestamp)
        {
            this._startTime = startTime;
            this._startTimestamp = startTimestamp;
            logger.LogTrace(GeexboxEventId.ApolloTracing, "operation started.");
        }

        public void SetParsingResult(long startTimestamp, long endTimestamp)
        {
            this._parsingResult = new ObjectResult();
            this._parsingResult.EnsureCapacity(2);
            this._parsingResult.SetValueUnsafe(0, "startOffset", startTimestamp - this._startTimestamp);
            this._parsingResult.SetValueUnsafe(1, "duration", endTimestamp - startTimestamp);
        }

        public void SetValidationResult(long startTimestamp, long endTimestamp)
        {
            this._validationResult = new ObjectResult();
            this._validationResult.EnsureCapacity(2);
            this._validationResult.SetValueUnsafe(0, "startOffset", startTimestamp - this._startTimestamp);
            this._validationResult.SetValueUnsafe(1, "duration", endTimestamp - startTimestamp);
        }

        public void AddResolverResult(GeexTracingResolverRecord record) => this._resolverRecords.Enqueue(record);

        public void SetOperationEndTime(IRequestContext context, TimeSpan duration)
        {
            this._duration = duration;
            if (context.Result is IQueryResult result)
            {
                var resultMap = this.Build();
                this.logger.LogTraceWithData(GeexboxEventId.ApolloTracing, null, resultMap);
                // 此处需要跳过introspection查询结果
                this.logger.LogDebugWithData(new EventId((nameof(GeexTracingOperationScope) + "End").GetHashCode(), nameof(GeexTracingOperationScope) + "End"), "Request ended.", new { QueryId = context.Request.QueryId, Data = (object)(context.Request.OperationName?.Contains("introspection") == true ? "[Schema Doc]" : result.Data)!, Error = result.Errors });
                context.Result = QueryResultBuilder.FromResult(result).AddExtension("tracing", resultMap).Create();
            }
        }

        public ObjectResult Build()
        {
            if (this._parsingResult == null)
                this.SetParsingResult(this._startTimestamp, this._startTimestamp);
            if (this._validationResult == null)
                this.SetValidationResult(this._startTimestamp, this._startTimestamp);
            var resultMap1 = new ObjectResult();
            resultMap1.EnsureCapacity(1);
            resultMap1.SetValueUnsafe(0, "resolvers", this.BuildResolverResults());
            ObjectResult resultMap2 = new ObjectResult();
            resultMap2.EnsureCapacity(7);
            resultMap2.SetValueUnsafe(0, "version", 1);
            resultMap2.SetValueUnsafe(1, "startTime", this._startTime.ToUnixTimeSeconds());
            resultMap2.SetValueUnsafe(2, "endTime", this._startTime.Add(this._duration).ToUnixTimeSeconds());
            resultMap2.SetValueUnsafe(3, "duration", this._duration.TotalSeconds);
            resultMap2.SetValueUnsafe(4, "parsing", (object)this._parsingResult);
            resultMap2.SetValueUnsafe(5, "validation", (object)this._validationResult);
            resultMap2.SetValueUnsafe(6, "execution", resultMap1);
            return resultMap2;
        }

        private ObjectResult[] BuildResolverResults()
        {
            int num = 0;
            ObjectResult[] resultMapArray = new ObjectResult[this._resolverRecords.Count];
            foreach (GeexTracingResolverRecord resolverRecord in this._resolverRecords)
            {
                ObjectResult resultMap = new ObjectResult();
                resultMap.EnsureCapacity(6);
                resultMap.SetValueUnsafe(0, "path", resolverRecord.Path);
                resultMap.SetValueUnsafe(1, "parentType", resolverRecord.ParentType);
                resultMap.SetValueUnsafe(2, "fieldName", resolverRecord.FieldName);
                resultMap.SetValueUnsafe(3, "returnType", resolverRecord.ReturnType);
                resultMap.SetValueUnsafe(4, "startOffset", resolverRecord.StartTimestamp - this._startTimestamp);
                resultMap.SetValueUnsafe(5, "duration", resolverRecord.EndTimestamp - resolverRecord.StartTimestamp);
                resultMapArray[num++] = resultMap;
            }
            return resultMapArray;
        }
    }

    internal class GeexTracingResolverRecord
    {
        public GeexTracingResolverRecord(
          IResolverContext context,
          long startTimestamp,
          long endTimestamp)
        {
            this.Path = context.Path.ToList();
            this.ParentType = context.ObjectType.Name;
            this.FieldName = context.Selection.Field.Name;
            this.ReturnType = context.Selection.Field.Type.TypeName();
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
        }

        public IReadOnlyList<object> Path { get; }

        public string ParentType { get; }

        public string FieldName { get; }

        public string ReturnType { get; }

        public long StartTimestamp { get; }

        public long EndTimestamp { get; }
    }
}
