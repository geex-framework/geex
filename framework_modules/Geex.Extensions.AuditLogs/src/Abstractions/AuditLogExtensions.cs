using System;
using System.Collections.Generic;
using Geex.Gql;
using Geex.Gql.Types;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Extensions.AuditLogs
{
    public static class AuditLogExtensions
    {
        internal const string FieldAuditKey = "Geex.AuditLogs.FieldEnabled";
        private const string TypeAuditLevelsKey = "Geex.AuditLogs.TypeLevels";

        public static IObjectTypeDescriptor<T> AuditFieldsImplicitly<T>(this IObjectTypeDescriptor<T> descriptor,
            AuditLevel level = AuditLevel.All) where T : ObjectTypeExtension
        {
            var rootExtensionType = typeof(T);
            if (!rootExtensionType.IsAssignableTo<MutationExtension<T>>() &&
                !rootExtensionType.IsAssignableTo<QueryExtension<T>>() &&
                !rootExtensionType.IsAssignableTo<SubscriptionExtension<T>>())
            {
                throw new InvalidOperationException("Only root type fields can be audited.");
            }
            if (!Enum.IsDefined(level))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            descriptor.Extend().OnBeforeCreate((context, definition) =>
            {
                if (!context.ContextData.TryGetValue(TypeAuditLevelsKey, out var configured))
                {
                    configured = new Dictionary<Type, AuditLevel>();
                    context.ContextData[TypeAuditLevelsKey] = configured;
                }
                ((Dictionary<Type, AuditLevel>)configured!)[rootExtensionType] = level;
                foreach (var field in definition.Fields)
                {
                    field.ContextData[GeexTypeInterceptor.RootFieldDeclaringTypeKey] = rootExtensionType;
                }
            });
            return descriptor;
        }

        public static IObjectFieldDescriptor Audit(this IObjectFieldDescriptor fieldDescriptor, bool enabled = true)
        {
            fieldDescriptor.Extend().OnBeforeCreate((_, definition) =>
            {
                definition.ContextData[FieldAuditKey] = enabled;
                Utils.AuditLogsTypeInterceptor.SetAuditDirective(definition, enabled);
            });
            return fieldDescriptor;
        }

        internal static AuditLevel? GetTypeAuditLevel(IDescriptorContext context, Type? declaringType)
        {
            if (declaringType != null &&
                context.ContextData.TryGetValue(TypeAuditLevelsKey, out var configured) &&
                configured is Dictionary<Type, AuditLevel> levels &&
                levels.TryGetValue(declaringType, out var level))
            {
                return level;
            }
            return null;
        }
    }
}
