using HotChocolate.Types;
using Volo.Abp.DependencyInjection;

namespace Geex.Gql.Types
{
    public abstract class QueryExtension<T> : ObjectTypeExtension<T>, IScopedDependency where T : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Query);
            base.Configure(descriptor);
        }
    }
    public abstract class MutationExtension<T> : ObjectTypeExtension<T>, IScopedDependency where T : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Mutation);
            base.Configure(descriptor);
        }
    }
    public abstract class SubscriptionExtension<T> : ObjectTypeExtension<T>, IScopedDependency where T : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Name(OperationTypeNames.Subscription);
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
