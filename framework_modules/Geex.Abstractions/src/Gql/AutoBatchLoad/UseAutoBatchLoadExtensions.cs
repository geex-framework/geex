using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql.AutoBatchLoad
{
    public static class UseAutoBatchLoadExtensions
    {
        /// <summary>
        /// 为单个字段启用/禁用 AutoBatchLoad。字段级 true 会覆盖全局 <see cref="GeexCoreModuleOptions.AutoBatchLoad"/> 的 false。
        /// </summary>
        public static IObjectFieldDescriptor UseAutoBatchLoad(this IObjectFieldDescriptor descriptor, bool enabled = true)
        {
            if (enabled)
            {
                descriptor.Extend().OnBeforeCreate((_, definition) =>
                {
                    definition.ApplyAutoBatchLoadMiddleware();
                });
            }

            return descriptor;
        }

        /// <summary>
        /// 为整个操作类型（Query/Mutation/Subscription）下的所有 eligible 字段批量启用/禁用 AutoBatchLoad。
        /// 字段级 <see cref="UseAutoBatchLoad(IObjectFieldDescriptor, bool)"/> 优先于此设置。
        /// </summary>
        public static IObjectTypeDescriptor UseAutoBatchLoad(this IObjectTypeDescriptor descriptor, bool enabled = true)
        {
            descriptor.Extend().OnBeforeCreate((_, definition) =>
            {
                if (!definition.IsOperationExtensionType())
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("UseAutoBatchLoad is only allowed on Query, Mutation, or Subscription operation types.")
                            .Build());
                }

                definition.GeexFeatures.AutoBatchLoad.Enabled = enabled;
            });

            return descriptor;
        }

        /// <summary>
        /// 为整个操作类型（Query/Mutation/Subscription）下的所有 eligible 字段批量启用/禁用 AutoBatchLoad。
        /// 字段级 <see cref="UseAutoBatchLoad(IObjectFieldDescriptor, bool)"/> 优先于此设置。
        /// </summary>
        public static IObjectTypeDescriptor<T> UseAutoBatchLoad<T>(this IObjectTypeDescriptor<T> descriptor, bool enabled = true)
        {
            ((IObjectTypeDescriptor)descriptor).UseAutoBatchLoad(enabled);
            return descriptor;
        }
    }
}
