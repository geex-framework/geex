using System;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Approbation;
using HotChocolate.Types;
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

            if (typeof(T).IsAssignableTo<IHasApproveMutation>())
            {
                var mutationType = this.GetType().GetInterface("IHasApproveMutation`1");
                var entityType = mutationType.GenericTypeArguments[0];
                var name = entityType.Name;
                var submit = mutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Submit));
                var approve = mutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Approve));
                var unsubmit = mutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnSubmit));
                var unApprove = mutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnApprove));
                var submitFieldDescriptor = descriptor.Field("submit" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (submit.Invoke(this,
                            new object?[] { context.Service<IMediator>(), context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark") }) as Task<bool>);
                    });
                var approveFieldDescriptor = descriptor.Field("approve" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (approve.Invoke(this,
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
                var unApproveFieldDescriptor = descriptor.Field("unApprove" + entityType.Name.RemovePreFix("I"))
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unApprove.Invoke(this,
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
