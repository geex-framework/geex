using System;
using System.Threading.Tasks;
using Geex.ApprovalFlows;
using HotChocolate.Types;
using Volo.Abp.DependencyInjection;

namespace Geex.Gql.Types
{
    public abstract class QueryExtension<T> : ObjectTypeExtension<T>, IScopedDependency where T : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Query);
            if (typeof(T).IsAssignableTo<ObjectTypeExtension>())
            {
                descriptor.IgnoreExtensionFields();
            }
            base.Configure(descriptor);
        }
    }
    public abstract class MutationExtension<T> : ObjectTypeExtension<T>, IScopedDependency where T : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            var mutationType = typeof(T);
            descriptor.Name(OperationTypeNames.Mutation);
            if (mutationType.IsAssignableTo<ObjectTypeExtension>())
            {
                descriptor.IgnoreExtensionFields();
            }

            if (mutationType.IsAssignableTo<IHasApproveMutation>())
            {
                var hasApproveMutationType = mutationType.GetInterface("IHasApproveMutation`1");
                var entityType = hasApproveMutationType.GenericTypeArguments[0];
                var entityName = entityType.Name;
                if (entityName.StartsWith("I") && char.IsUpper(entityName[1]))
                {
                    entityName = entityName[1..];
                }
                var submit = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Submit));
                var approve = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Approve));
                var unSubmit = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnSubmit));
                var unApprove = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnApprove));
                var submitFieldDescriptor = descriptor.Field("submit" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (submit.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    });
                var approveFieldDescriptor = descriptor.Field("approve" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (approve.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    });
                var unSubmitFieldDescriptor = descriptor.Field("unSubmit" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unSubmit.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    })
                    ;
                var unApproveFieldDescriptor = descriptor.Field("unApprove" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unApprove.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    });
                if (GeexTypeInterceptor.AuditTypes.Contains(mutationType))
                {
                    submitFieldDescriptor.Directive("audit");
                    approveFieldDescriptor.Directive("audit");
                    unSubmitFieldDescriptor.Directive("audit");
                    unApproveFieldDescriptor.Directive("audit");
                }
                base.Configure(descriptor);
            }
            base.Configure(descriptor);
        }
    }
    public abstract class SubscriptionExtension<T> : ObjectTypeExtension<T>, IScopedDependency where T : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Subscription);
            if (typeof(T).IsAssignableTo<ObjectTypeExtension>())
            {
                descriptor.IgnoreExtensionFields();
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
