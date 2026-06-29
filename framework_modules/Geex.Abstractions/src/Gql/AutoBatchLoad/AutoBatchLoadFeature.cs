using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql.AutoBatchLoad
{
    public static class AutoBatchLoadFeature
    {
        public const string OperationContextDataKey = "Geex.AutoBatchLoad.OperationEnabled";
        public const string FieldContextDataKey = "Geex.AutoBatchLoad.FieldEnabled";
        public const string MiddlewareKey = "Geex.AutoBatchLoad.Middleware";

        public static bool IsAutoBatchLoadEnabled(ITypeCompletionContext completionContext, ObjectTypeDefinition operationType)
        {
            if (operationType.ContextData.TryGetValue(OperationContextDataKey, out var value) && value is bool enabled)
            {
                return enabled;
            }

            return completionContext.Services.GetService(typeof(GeexCoreModuleOptions)) is GeexCoreModuleOptions options
                ? options.AutoBatchLoad
                : true;
        }

        public static bool? GetOperationOverride(ObjectTypeDefinition operationType)
        {
            if (operationType.ContextData.TryGetValue(OperationContextDataKey, out var value) && value is bool enabled)
            {
                return enabled;
            }

            return null;
        }
    }
}
