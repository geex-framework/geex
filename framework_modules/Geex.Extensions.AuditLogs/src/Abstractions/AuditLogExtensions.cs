using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using System.Reflection;
using Geex.Gql;
using Geex.Gql.Types;

namespace Geex.Extensions.AuditLogs
{
    public static class AuditLogExtensions
    {
        public static IObjectTypeDescriptor<T> AuditFieldsImplicitly<T>(this IObjectTypeDescriptor<T> descriptor) where T : ObjectTypeExtension
        {
            var rootExtensionType = typeof(T);
            GeexTypeInterceptor.AuditTypes.AddIfNotContains(rootExtensionType);
            if (rootExtensionType.IsAssignableTo<MutationExtension<T>>() || rootExtensionType.IsAssignableTo<QueryExtension<T>>() || rootExtensionType.IsAssignableTo<SubscriptionExtension<T>>())
            {
                var methods = rootExtensionType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).AsEnumerable();
                methods = methods.Where(x => x is { IsSpecialName: false });
                foreach (var methodInfo in methods)
                {
                    descriptor.Field(methodInfo).Audit();
                }
                return descriptor;
            }
            throw new InvalidOperationException("Only root type fields can be audited.");
        }

        public static IObjectFieldDescriptor Audit(this IObjectFieldDescriptor fieldDescriptor)
        {
            fieldDescriptor = fieldDescriptor.Directive<AuditDirectiveType>();
            return fieldDescriptor;
        }
    }
}
