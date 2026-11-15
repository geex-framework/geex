using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace Geex.Extensions.AuditLogs.Utils;

internal sealed class AuditErrorListener : ExecutionDiagnosticEventListener
{
    internal const string ContextKey = "Geex.AuditLogs.FieldErrors";

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        if (context.LocalContextData.TryGetValue(ContextKey, out var value) && value is List<IError> errors)
        {
            errors.Add(error);
        }
    }
}
