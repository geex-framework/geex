using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace Geex.Gql.AutoBatchLoad
{
    public static class UseAutoBatchLoadExtensions
    {
        public static IObjectTypeDescriptor UseAutoBatchLoad(this IObjectTypeDescriptor descriptor, bool enabled = true)
        {
            descriptor.Extend().OnBeforeCreate((_, definition) =>
            {
                if (!AutoBatchLoadGraphQL.IsOperationObjectType(definition.RuntimeType, definition.Name))
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("UseAutoBatchLoad is only allowed on Query, Mutation, or Subscription operation types.")
                            .Build());
                }

                definition.ContextData[AutoBatchLoadFeature.OperationContextDataKey] = enabled;
            });

            return descriptor;
        }

        public static IObjectTypeDescriptor<T> UseAutoBatchLoad<T>(this IObjectTypeDescriptor<T> descriptor, bool enabled = true)
        {
            ((IObjectTypeDescriptor)descriptor).UseAutoBatchLoad(enabled);
            return descriptor;
        }
    }
}
