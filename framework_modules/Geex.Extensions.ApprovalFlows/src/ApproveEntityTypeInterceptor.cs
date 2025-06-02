using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Geex.Gql;
using Geex.Gql.Types;
using Geex.Storage;

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using MongoDB.Entities;

namespace Geex.Extensions.ApprovalFlows
{
    public class ApproveEntityTypeInterceptor : TypeInterceptor
    {
        public Dictionary<Type, string> PatchedEntities = new Dictionary<Type, string>();
        /// <inheritdoc />
        public override void OnAfterMergeTypeExtensions()
        {
            var hasApproveMutationTypes = GeexModule.RootTypes.Where(x => x.IsAssignableTo<IHasApproveMutation>());
            foreach (var rootType in hasApproveMutationTypes)
            {
                var runtimeType = rootType;
                // Apply entity configuration using reflection to call the generic ConfigEntity method
                //Type descriptorType = typeof(IObjectTypeDescriptor<>).MakeGenericType(runtimeType);
                //var objectTypeDescriptor = Activator.CreateInstance(descriptorType).As<IObjectTypeDescriptor<IHasApproveMutation>>();
                var approveMutationType = runtimeType.GetInterfaces().First(x => x.Name.StartsWith(
                    $"{nameof(IHasApproveMutation)}`1"));
                var entityType = approveMutationType.GenericTypeArguments[0];
                var entityName = entityType.Name;
                if (entityName.StartsWith("I") && char.IsUpper(entityName[1]))
                {
                    entityName = entityName[1..];
                }
                PatchedEntities.Add(approveMutationType, entityName);
            }
            base.OnAfterMergeTypeExtensions();
        }

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
                //Type descriptorType = typeof(IObjectTypeDescriptor<>).MakeGenericType(runtimeType);
                //var objectTypeDescriptor = Activator.CreateInstance(descriptorType).As<IObjectTypeDescriptor<IApproveEntity>>();
                objectTypeDefinition.Fields.Add(
                    new ObjectFieldDefinition(nameof(IApproveEntity.ApproveStatus),
                        type: completionContext.TypeInspector.GetTypeRef(typeof(ApproveStatus)),
                        pureResolver: context => context.Parent<IApproveEntity>().ApproveStatus));
                objectTypeDefinition.Fields.Add(
                    new ObjectFieldDefinition(nameof(IApproveEntity.Submittable),
                        type: TypeReference.Parse("Boolean"),
                        pureResolver: context => context.Parent<IApproveEntity>().Submittable));
                //objectTypeDescriptor.Field(x => ((IApproveEntity)x).ApproveStatus);
                //objectTypeDescriptor.Field(x => ((IApproveEntity)x).Submittable);
            }

            if (typeof(Mutation).IsAssignableFrom(runtimeType))
            {

                foreach (var (mutationExtType, entityName) in PatchedEntities)
                {
                    var hasApproveMutationType = mutationExtType.GetInterface(nameof(IHasApproveMutation));
                    var submit = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Submit));
                    var approve = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.Approve));
                    var unSubmit = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnSubmit));
                    var unApprove = hasApproveMutationType.GetMethod(nameof(IHasApproveMutation<IApproveEntity>.UnApprove));
                    List<MemberInfo> methods = [submit, approve, unSubmit, unApprove];
                    foreach (var method in methods)
                    {
                        var fieldDefinition = new ObjectFieldDefinition($"{method.Name.ToCamelCase()}{entityName}",
                            type: TypeReference.Parse("Boolean"),
                            resolver: async (context) =>
                                await (submit.Invoke(this, [
                                    context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"),
                                    context.Service<IUnitOfWork>()
                                ]) as Task<bool>));
                        fieldDefinition.Arguments.Add(new InputFieldDefinition("ids", type: TypeReference.Parse("String[]")));
                        fieldDefinition.Arguments.Add(new InputFieldDefinition("remark", type: TypeReference.Parse("String")));
                        if (GeexTypeInterceptor.AuditTypes.Contains(mutationExtType))
                        {
                            fieldDefinition.Directives.Add(new DirectiveDefinition(new DirectiveNode("audit")));
                        }
                        objectTypeDefinition.Fields.Add(fieldDefinition);
                    }

                    //objectTypeDefinition.Fields.Add(fieldDefinition);
                    //var submitFieldDescriptor = objectTypeDescriptor.Field($"submit{entityName}")
                    //    .Type<BooleanType>()
                    //    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    //    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    //    .Resolve(resolver: async (context, token) =>
                    //    {
                    //        return await (submit.Invoke(this,
                    //            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    //    });
                    //var approveFieldDescriptor = objectTypeDescriptor.Field($"approve{entityName}")
                    //    .Type<BooleanType>()
                    //    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    //    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    //    .Resolve(resolver: async (context, token) =>
                    //    {
                    //        return await (approve.Invoke(this,
                    //            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    //    });
                    //var unSubmitFieldDescriptor = objectTypeDescriptor.Field($"unSubmit{entityName}")
                    //    .Type<BooleanType>()
                    //    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    //    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    //    .Resolve(resolver: async (context, token) =>
                    //    {
                    //        return await (unSubmit.Invoke(this,
                    //            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    //    })
                    //    ;
                    //var unApproveFieldDescriptor = objectTypeDescriptor.Field($"unApprove{entityName}")
                    //    .Type<BooleanType>()
                    //    .Argument("ids", argumentDescriptor => argumentDescriptor.Type(typeof(string[])))
                    //    .Argument("remark", argumentDescriptor => argumentDescriptor.Type(typeof(string)))
                    //    .Resolve(resolver: async (context, token) =>
                    //    {
                    //        return await (unApprove.Invoke(this,
                    //            new object?[] { context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"), context.Service<IUnitOfWork>() }) as Task<bool>);
                    //    });

                }

            }

            base.OnBeforeCompleteType(completionContext, definition);
        }
    }
}
