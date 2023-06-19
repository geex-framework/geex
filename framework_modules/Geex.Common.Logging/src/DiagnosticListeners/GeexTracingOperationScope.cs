using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Elastic.Apm;
using Elastic.Apm.Api;

using Geex.Common.Abstractions;

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Gql
{
    internal class GeexTracingOperationScope : IDisposable
    {
        private readonly IRequestContext _context;
        private readonly ILogger<GeexTracingDiagnosticEventListener> _logger;
        private readonly DateTime _startTime;
        private readonly GeexTracingResultBuilder _builder;
        private readonly ITimestampProvider _timestampProvider;
        private readonly string _env;
        private readonly ISpan? _span;
        private bool _disposed;

        public GeexTracingOperationScope(IRequestContext context,
            ILogger<GeexTracingDiagnosticEventListener> logger,
            DateTime startTime,
            GeexTracingResultBuilder builder,
            ITimestampProvider timestampProvider)
        {
            this._context = context;
            this._logger = logger;
            this._startTime = startTime;
            this._builder = builder;
            this._timestampProvider = timestampProvider;
            this._env = context.Services.GetService<IWebHostEnvironment>()?.EnvironmentName ?? "[unknown]";
            if (Agent.IsConfigured)
            {
                context.ContextData["ApmTransaction"] = Agent.Tracer.CurrentTransaction;
                context.ContextData["ApmOperationSpan"] = this._span = Agent.Tracer.CurrentTransaction?.StartSpan(context.GetOperationDisplay(), "request", "operation");
            }

            builder.SetOperationStartTime(startTime, timestampProvider.NowInNanoseconds());
        }

        public void Dispose()
        {
            if (this._disposed)
                return;
            var operationType = this._context.Operation?.Type.ToString();
            var operationName = this._context.Request.OperationName ?? "[anonymous]";
            var traceInfo = new GqlTraceInfo(
                _env,
                this._context.Request.QueryId,
                this._context.DocumentId,
                this._context.DocumentHash,
                this._context.IsValidDocument,
                this._context.OperationId,
                operationName,
                operationType
            );
            this.WriteTransaction(this._context, traceInfo);
            this._builder.SetOperationEndTime(this._context, this._timestampProvider.UtcNow() - this._startTime);

            this._disposed = true;
        }

        private void WriteTransaction(IRequestContext context, GqlTraceInfo traceInfo)
        {
            if (!Agent.IsConfigured)
                return;

            var transaction = context.ContextData["ApmTransaction"] as ITransaction;
            if (transaction == null)
            {
                return;
            }
            try
            {
                transaction.Name = traceInfo.OperationType + " " + traceInfo.OperationName;
                transaction.Type = "request";
                transaction.SetLabel("env", traceInfo.Env);
                transaction.SetLabel("graphql.document.id", traceInfo.DocumentId);
                transaction.SetLabel("graphql.document.hash", traceInfo.DocumentHash);
                transaction.SetLabel("graphql.document.valid", traceInfo.IsValidDocument);
                transaction.SetLabel("graphql.operation.id", traceInfo.OperationId);
                transaction.SetLabel("graphql.operation.name", traceInfo.OperationName);
                transaction.SetLabel("graphql.operation.kind", traceInfo.OperationType);
                this._span?.End();
                if (context.Result is IQueryResult result)
                {
                    var errors = result.Errors;
                    if (errors?.Any() == true)
                    {
                        transaction.SetLabel("graphql.errors.count", errors.Count);
                        foreach (var error in result.Errors)
                        {
                            if (error.Exception != default)
                            {
                                Agent.Tracer.CaptureException(error.Exception);
                            }
                            else
                            {
                                Agent.Tracer.CaptureError(error.Message, error.Code);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Agent.Tracer.CaptureErrorLog(new ErrorLog("EnrichTransaction failed."), exception: ex);
            }
        }
    }

    internal record GqlTraceInfo(string Env, string? QueryId, string? DocumentId, string? DocumentHash, bool IsValidDocument, string? OperationId, string OperationName, string? OperationType);
}
