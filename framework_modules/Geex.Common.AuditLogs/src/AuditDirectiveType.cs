using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authentication;

using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.AuditLogs
{
    public class ApproveDirectiveType
    {

        public class Config : GqlConfig.Directive<ApproveDirectiveType>
        {
            /// <inheritdoc />
            protected override void Configure(IDirectiveTypeDescriptor<ApproveDirectiveType> descriptor)
            {
                descriptor.Name("approve");
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Use((next, directive) => async context =>
                {
                    var user = context.Service<ICurrentUser>();
                    var type = context.Operation.Type;
                    var operation = context.Operation.Document.ToString();
                    var valueTask = next.Invoke(context);
                    await valueTask.AsTask()
                        .ContinueWith(async task =>
                    {
                        try
                        {
                            var uow = context.Service<IUnitOfWork>();
                            var logEntry = new AuditLog()
                            {
                                OperationType = type,
                                OperatorId = user.UserId,
                                Query = operation,
                            };
                            if (task.IsFaulted)
                            {
                                // Handle the exception
                                var innerExceptions = task.Exception.InnerExceptions;
                                logEntry.Result = JsonNode.Parse(innerExceptions.Count == 1
                                    ? innerExceptions[0].ToJson()
                                    : innerExceptions.ToJson());
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
                            context.Service<ILogger<ApproveDirectiveType>>().LogError(e, "AuditLog failed with exception.");
                        }
                    });
                });
                base.Configure(descriptor);
            }
        }
    }
}
