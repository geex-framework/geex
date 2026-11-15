using System;
using System.Text.Json.Nodes;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.Authentication;
using Geex.Storage;
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
                            Result = JsonNode.Parse(failure == null
                                ? context.Result.ToJson()
                                : failure.ToExceptionModel().ToJson()),
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
