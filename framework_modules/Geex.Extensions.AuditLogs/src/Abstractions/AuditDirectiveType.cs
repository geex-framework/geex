﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.Authentication;
using Geex.Storage;
using HotChocolate.Language;
using HotChocolate.Types;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
                    var mainTask = next.Invoke(context).AsTask();
                    await mainTask.ContinueWith(async task =>
                    {
                        try
                        {
                            using var uow = new GeexDbContext(context.RequestServices);
                            var logEntry = new AuditLog()
                            {
                                OperationType = operationType,
                                OperationName = operationName,
                                OperatorId = user.UserId,
                                Operation = operation,
                                Variables = JsonNode.Parse(variables),
                                ClientIp = clientIp,
                            };
                            if (task.IsFaulted)
                            {
                                // Handle the exception
                                var innerExceptions = task.Exception.InnerExceptions;
                                logEntry.Result = JsonNode.Parse(innerExceptions.Count == 1
                                    ? innerExceptions[0].ToExceptionModel().ToJson()
                                    : innerExceptions.Select(x => x.ToExceptionModel()).ToJson());
                                logEntry.IsSuccess = false;
                            }
                            else if (task.IsCompleted)
                            {
                                logEntry.Result = JsonNode.Parse(context.Result.ToJson());
                                logEntry.IsSuccess = true;
                            }
                            uow?.Attach(logEntry);
                            await uow.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            context.Service<ILogger<AuditDirectiveType>>().LogError(e, "AuditLog failed with exception.");
                        }
                    });
                    if (mainTask.IsFaulted && mainTask.Exception.InnerException != default)
                    {
                        context.ReportError(mainTask.Exception.InnerException);
                    }
                });

                base.Configure(descriptor);
            }
        }
    }
}
