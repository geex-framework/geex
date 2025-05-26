using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Geex.ApprovalFlows;
using Geex.Gql;
using Geex.Storage;

using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Extensions.ApprovalFlows
{
    public class ApproveEntityTypeInterceptor : TypeInterceptor
    {
        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is not ObjectTypeDefinition objectTypeDefinition)
            {
                base.OnBeforeCompleteType(completionContext, definition);
                return;
            }

            var runtimeType = objectTypeDefinition.RuntimeType;
            if (typeof(IApproveEntity).IsAssignableFrom(runtimeType))
            {
                // Apply entity configuration using reflection to call the generic ConfigEntity method
                Type descriptorType = typeof(IObjectTypeDescriptor<>).MakeGenericType(runtimeType);
                var objectTypeDescriptor = Activator.CreateInstance(descriptorType).As<IObjectTypeDescriptor<IApproveEntity>>();
                objectTypeDescriptor.Field(x => ((IApproveEntity)x).ApproveStatus);
                objectTypeDescriptor.Field(x => ((IApproveEntity)x).Submittable);
            }

            if (typeof(IHasApproveMutation).IsAssignableFrom(runtimeType))
            {
                // Apply entity configuration using reflection to call the generic ConfigEntity method
                Type descriptorType = typeof(IObjectTypeDescriptor<>).MakeGenericType(runtimeType);
                var objectTypeDescriptor = Activator.CreateInstance(descriptorType).As<IObjectTypeDescriptor<IHasApproveMutation>>();
                var approveMutationType = runtimeType.GetInterfaces().First(x => x.Name.StartsWith(nameof(IHasApproveMutation) + "`1"));
                var approvePropertyList = approveMutationType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var entityType = approveMutationType.GenericTypeArguments[0];
                foreach (var item in approvePropertyList)
                {
                    var fieldName = item.Name.ToCamelCase() + entityType.Name.RemovePreFix("I");
                    objectTypeDescriptor.Field(fieldName).Type<BooleanType>();
                }
                var hasApproveMutationType = runtimeType.GetInterface("IHasApproveMutation`1");
                var entityName = entityType.Name;
                if (entityName.StartsWith("I") && char.IsUpper(entityName[1]))
                {
                    entityName = entityName[1..];
                }
                var submit = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Submit));
                var approve = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Approve));
                var unSubmit = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnSubmit));
                var unApprove = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnApprove));
                var submitFieldDescriptor = objectTypeDescriptor.Field("submit" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (submit.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    });
                var approveFieldDescriptor = objectTypeDescriptor.Field("approve" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (approve.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    });
                var unSubmitFieldDescriptor = objectTypeDescriptor.Field("unSubmit" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unSubmit.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    })
                    ;
                var unApproveFieldDescriptor = objectTypeDescriptor.Field("unApprove" + entityName)
                    .Type<BooleanType>()
                    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    .Resolve(resolver: async (context, token) =>
                    {
                        return await (unApprove.Invoke(this,
                            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    });
                if (GeexTypeInterceptor.AuditTypes.Contains(runtimeType))
                {
                    submitFieldDescriptor.Directive("audit");
                    approveFieldDescriptor.Directive("audit");
                    unSubmitFieldDescriptor.Directive("audit");
                    unApproveFieldDescriptor.Directive("audit");
                }
            }


            base.OnBeforeCompleteType(completionContext, definition);
        }
    }
}
