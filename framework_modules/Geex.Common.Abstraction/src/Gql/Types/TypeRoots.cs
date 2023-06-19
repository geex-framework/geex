using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Auditing;

using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

using MediatR;

using Volo.Abp.DependencyInjection;

namespace Geex.Common.Abstraction.Gql.Types
{
    public abstract class QueryExtension<T> : ObjectTypeExtension<T>, IScopedDependency
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Query);
            if (typeof(T).IsAssignableTo<ObjectTypeExtension>())
            {
                descriptor.ConfigExtensionFields();
            }
            base.Configure(descriptor);
        }
    }
    public abstract class MutationExtension<T> : ObjectTypeExtension<T>, IScopedDependency
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Mutation);
            if (typeof(T).IsAssignableTo<ObjectTypeExtension>())
            {
                descriptor.ConfigExtensionFields();
            }

            if (typeof(T).IsAssignableTo<IHasAuditMutation>())
            {
                var mutationType = this.GetType().GetInterface("IHasAuditMutation`1");
                var entityType = mutationType.GenericTypeArguments[0];
                var name = entityType.Name;
                var submit = mutationType.GetMethod(nameof(IHasAuditMutation<IAuditEntity>.Submit));
                var audit = mutationType.GetMethod(nameof(IHasAuditMutation<IAuditEntity>.Audit));
                var unsubmit = mutationType.GetMethod(nameof(IHasAuditMutation<IAuditEntity>.UnSubmit));
                var unaudit = mutationType.GetMethod(nameof(IHasAuditMutation<IAuditEntity>.UnAudit));
                var submitFieldDescriptor = descriptor.Field("submit" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (submit.Invoke(this,
                            new object?[] { context.Service<IMediator>(), context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark") }) as Task<bool>);
                    });
                var auditFieldDescriptor = descriptor.Field("audit" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (audit.Invoke(this,
                            new object?[] { context.Service<IMediator>(), context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark") }) as Task<bool>);
                    });
                var unsubmitFieldDescriptor = descriptor.Field("unsubmit" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unsubmit.Invoke(this,
                            new object?[] { context.Service<IMediator>(), context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark") }) as Task<bool>);
                    })
                    ;
                var unauditFieldDescriptor = descriptor.Field("unaudit" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unaudit.Invoke(this,
                            new object?[] { context.Service<IMediator>(), context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark") }) as Task<bool>);
                    });
                base.Configure(descriptor);
            }
            base.Configure(descriptor);
        }
    }
    public abstract class SubscriptionExtension<T> : ObjectTypeExtension<T>, IScopedDependency
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Subscription);
            if (typeof(T).IsAssignableTo<ObjectTypeExtension>())
            {
                descriptor.ConfigExtensionFields();
            }

            base.Configure(descriptor);
        }
    }

    public class Query
    {
    }
    public class Mutation
    {
    }

    public class Subscription
    {
    }
}
