using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Authorization;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Geex.Common.Abstraction.Approbation;
using Geex.Common.Abstraction.Gql.Types;

namespace Geex.Common.AuditLogs
{
    public static class AuditLogExtensions
    {
        public static IObjectTypeDescriptor<T> AuditFieldsImplicitly<T>(this IObjectTypeDescriptor<T> descriptor) where T : class
        {
            var propertyList = descriptor.GetFields();
            foreach (var item in propertyList)
            {
                item.Audit();
            }
            return descriptor;
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
