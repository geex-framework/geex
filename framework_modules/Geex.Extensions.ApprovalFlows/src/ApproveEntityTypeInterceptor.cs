using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Geex.Gql;
using Geex.Gql.Types;

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Extensions.ApprovalFlows
{
    public class ApproveEntityTypeInterceptor : TypeInterceptor
    {
        public Dictionary<Type, (Type implementation, string entityName)> PatchedEntities = new Dictionary<Type, (Type implementation, string entityName)>();
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
                PatchedEntities.TryAdd(approveMutationType, (runtimeType, entityName));
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
                objectTypeDefinition.Fields.Add(
                    new ObjectFieldDefinition(nameof(IApproveEntity.ApproveStatus),
                        type: completionContext.TypeInspector.GetTypeRef(typeof(ApproveStatus)),
                        pureResolver: context => context.Parent<IApproveEntity>().ApproveStatus));
                objectTypeDefinition.Fields.Add(
                    new ObjectFieldDefinition(nameof(IApproveEntity.Submittable),
                        type: TypeReference.Parse("Boolean"),
                        pureResolver: context => context.Parent<IApproveEntity>().Submittable));
            }

            if (typeof(Mutation).IsAssignableFrom(runtimeType))
            {

                foreach (var (mutationExtType, data) in PatchedEntities)
                {
                    var (implementation, entityName) = data;
                    var methods = mutationExtType
                        .GetMethods()
                        .Where(m => m.GetParameters() is { Length: 3 } parameters
                                    && parameters[0].ParameterType == typeof(string[])
                                    && parameters[2].ParameterType == typeof(IUnitOfWork))
                        .ToList();
                    foreach (var method in methods)
                    {
                        var capturedMethod = method;
                        var fieldDefinition = new ObjectFieldDefinition($"{capturedMethod.Name.ToCamelCase()}{entityName}",
                            type: TypeReference.Parse("Boolean"),
                            resolver: async (context) =>
                            {
                                var instance = context.Service(implementation);
                                return await (capturedMethod.Invoke(instance, [
                                    context.ArgumentValue<string[]>("ids"), context.ArgumentValue<string>("remark"),
                                    context.Service<IUnitOfWork>()
                                ]) as Task<bool>);
                            });
                        fieldDefinition.Arguments.Add(new InputFieldDefinition("ids", type: TypeReference.Parse("[String!]")));
                        fieldDefinition.Arguments.Add(new InputFieldDefinition("remark", type: TypeReference.Parse("String")));
                        if (GeexTypeInterceptor.AuditTypes.Contains(mutationExtType))
                        {
                            fieldDefinition.Directives.Add(new DirectiveDefinition(new DirectiveNode("audit")));
                        }
                        objectTypeDefinition.Fields.Add(fieldDefinition);
                    }
                }
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }
    }
}
