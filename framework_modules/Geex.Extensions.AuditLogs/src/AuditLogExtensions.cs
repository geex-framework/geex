using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
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

        public static IObjectFieldDescriptor Field<TMutation>(this IObjectTypeDescriptor<TMutation> descriptor,
            Expression<Func<IHasApproveMutation, Task<bool>>> propertyOrMethod) where TMutation : MutationExtension<TMutation>, IHasApproveMutation
        {
            var hasApproveMutationType = typeof(TMutation).GetInterface("IHasApproveMutation`1");
            var entityType = hasApproveMutationType.GenericTypeArguments[0];
            var entityName = entityType.Name;
            if (entityName.StartsWith("I") && char.IsUpper(entityName[1]))
            {
                entityName = entityName[1..];
            }
            var propName = propertyOrMethod.Body.As<MethodCallExpression>().Method.Name.ToCamelCase() + entityName;
            return descriptor.Field(propName);
        }
    }
}
