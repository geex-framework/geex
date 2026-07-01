using Geex.Gql;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Gql.AutoBatchLoad
{
    public static class UseAutoBatchLoadExtensions
    {
        private sealed class ConfigurationLog;

        /// <summary>
        /// 为单个字段启用/禁用 AutoBatchLoad。字段级 true 可覆盖全局 <see cref="GeexCoreModuleOptions.AutoBatchLoad"/> 的 false。
        /// </summary>
        public static IObjectFieldDescriptor UseAutoBatchLoad(this IObjectFieldDescriptor descriptor, bool enabled = true)
        {
            descriptor.Extend().OnBeforeCreate((_, definition) =>
            {
                ConfigureAutoBatchLoad(definition, enabled);
            });

            return descriptor;
        }

        /// <summary>
        /// 为整个操作类型（Query/Mutation/Subscription）下的所有字段批量启用/禁用 AutoBatchLoad。
        /// 与字段级 <see cref="UseAutoBatchLoad(IObjectFieldDescriptor, bool)"/> 同时配置时，以实际调用顺序为准。
        /// </summary>
        public static IObjectTypeDescriptor UseAutoBatchLoad(this IObjectTypeDescriptor descriptor, bool enabled = true)
        {
            descriptor.Extend().OnBeforeCreate((context, definition) =>
            {
                if (!definition.IsOperationExtensionType())
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("UseAutoBatchLoad is only allowed on Query, Mutation, or Subscription operation types.")
                            .Build());
                }

                var logger = context.Services.GetService<ILogger<ConfigurationLog>>();

                foreach (var fieldDefinition in definition.Fields)
                {
                    if (fieldDefinition.GeexFeatures.AutoBatchLoad?.IsEnabled is { } previous)
                    {
                        logger?.LogWarning(
                            "Operation 类型级 UseAutoBatchLoad({Enabled}) 覆盖了字段 {FieldName} 上的 UseAutoBatchLoad({Previous}) 配置；以实际调用顺序为准。",
                            enabled,
                            fieldDefinition.Name,
                            previous);
                    }

                    ConfigureAutoBatchLoad(fieldDefinition, enabled);
                }
            });

            return descriptor;
        }

        /// <summary>
        /// 为整个操作类型（Query/Mutation/Subscription）下的所有字段批量启用/禁用 AutoBatchLoad。
        /// 与字段级 <see cref="UseAutoBatchLoad(IObjectFieldDescriptor, bool)"/> 同时配置时，以实际调用顺序为准。
        /// </summary>
        public static IObjectTypeDescriptor<T> UseAutoBatchLoad<T>(this IObjectTypeDescriptor<T> descriptor, bool enabled = true)
        {
            ((IObjectTypeDescriptor)descriptor).UseAutoBatchLoad(enabled);
            return descriptor;
        }

        private static void ConfigureAutoBatchLoad(ObjectFieldDefinition definition, bool enabled)
        {
            var geexFeatures = definition.GeexFeatures;
            geexFeatures.AutoBatchLoad = new AutoBatchLoadFeatureConfig(enabled);
        }

        internal static void EnsureAutoBatchLoadConfigured(ObjectFieldDefinition definition, bool globalDefault)
        {
            if (definition.GeexFeatures.AutoBatchLoad is null)
            {
                ConfigureAutoBatchLoad(definition, globalDefault);
            }
        }
    }
}
