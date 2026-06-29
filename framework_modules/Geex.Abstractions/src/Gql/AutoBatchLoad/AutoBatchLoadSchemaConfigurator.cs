using Geex.Gql.Types;

using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Gql.AutoBatchLoad
{
    public static class AutoBatchLoadSchemaConfigurator
    {
        public static IObjectTypeDescriptor<T> ConfigureOperation<T>(
            IObjectTypeDescriptor<T> descriptor,
            bool autoBatchLoad)
        {
            descriptor.UseAutoBatchLoad(autoBatchLoad);
            return descriptor;
        }

    }
}
