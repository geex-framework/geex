using System;
using System.Reflection;

using System;
using System.Linq;

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
                if (!IsOperationType(definition.RuntimeType))
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

        private static bool IsOperationType(Type runtimeType) =>
            runtimeType == typeof(Query) ||
            runtimeType == typeof(Mutation) ||
            runtimeType == typeof(Subscription);
    }
}
