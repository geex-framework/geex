using System;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.AuditLogs.Utils;
using Geex.Extensions.Authentication;
using Geex.Storage;
using HotChocolate;
using HotChocolate.Types;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using DirectiveLocation = HotChocolate.Types.DirectiveLocation;


namespace Geex.Extensions.AuditLogs
{
    public class AuditDirectiveType
    {
        public const string DirectiveName = "audit";
        public class Config : GqlConfig.Directive<AuditDirectiveType>
        {
            /// <inheritdoc />
            protected override void Configure(IDirectiveTypeDescriptor<AuditDirectiveType> descriptor)
            {
                descriptor.Name(DirectiveName);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Use((next, directive) => async context =>
                {
                    var user = context.Service<ICurrentUser>();
                    var operationType = context.Operation.Type;
                    var operationName = context.Operation.Name;
                    var operation = context.Operation.Document.ToString();
                    var variables = context.Variables.ToJson();
                    var clientIp = context.Service<IHttpContextAccessor>()?.HttpContext?.Connection.RemoteIpAddress?.ToString();
                    var reportedErrors = new List<IError>();
                    var hadErrorState = context.LocalContextData.TryGetValue(AuditErrorListener.ContextKey, out var previousErrors);
                    context.LocalContextData = context.LocalContextData.SetItem(AuditErrorListener.ContextKey, reportedErrors);
                    Exception? failure = null;
                    try
                    {
                        await next.Invoke(context);
                    }
                    catch (Exception error)
                    {
                        failure = error;
                        context.ReportError(error);
                    }
                    finally
                    {
                        context.LocalContextData = hadErrorState
                            ? context.LocalContextData.SetItem(AuditErrorListener.ContextKey, previousErrors)
                            : context.LocalContextData.Remove(AuditErrorListener.ContextKey);
                    }

                    try
                    {
                        using var uow = new GeexDbContext(context.RequestServices);
                        var logEntry = new AuditLog
                        {
                            OperationType = operationType,
                            OperationName = operationName,
                            OperatorId = user.UserId,
                            Operation = operation,
                            Variables = JsonNode.Parse(variables),
                            ClientIp = clientIp,
                            Result = JsonNode.Parse(failure != null
                                ? failure.ToExceptionModel().ToJson()
                                : reportedErrors.Count > 0
                                    ? reportedErrors.Select(error => new
                                    {
                                        error.Message,
                                        error.Code,
                                        Path = error.Path?.ToString(),
                                        error.Extensions,
                                    }).ToJson()
                                    : context.Result.ToJson()),
                            IsSuccess = failure == null && !context.HasErrors,
                        };
                        uow.Attach(logEntry);
                        await uow.SaveChanges();
                    }
                    catch (Exception error)
                    {
                        context.Service<ILogger<AuditDirectiveType>>().LogError(error, "AuditLog failed with exception.");
                    }
                });

                base.Configure(descriptor);
            }
        }
    }
}
