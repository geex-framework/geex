using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Geex.Gql.Types;

using HotChocolate.Types;

namespace Geex.Extensions.ApprovalFlows
{
    public static class Extensions
    {

        public static IObjectFieldDescriptor Field<TMutation>(this IObjectTypeDescriptor<TMutation> descriptor,
            Expression<Func<IHasApproveMutation, Task<bool>>> propertyOrMethod) where TMutation : MutationExtension<TMutation>, IHasApproveMutation
        {
            var hasApproveMutationType = typeof(TMutation).GetInterface($"{nameof(IHasApproveMutation)}`1");
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
