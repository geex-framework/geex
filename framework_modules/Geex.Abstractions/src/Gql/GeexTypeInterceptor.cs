using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Geex.Gql.Extensions;
using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

using Microsoft.Extensions.Logging;

using MongoDB.Entities;

using Open.Collections;

namespace Geex.Gql
{
    public class GeexTypeInterceptor(ILogger<GeexTypeInterceptor> _logger) : TypeInterceptor
    {
        #region Static Configuration

        public static HashSet<Type> AuditTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> IgnoredTypes { get; } = new HashSet<Type>();
        public static HashSet<KeyValuePair<Type, Type>> OneOfConfigs { get; } = new HashSet<KeyValuePair<Type, Type>>();
        public Dictionary<Type, (Type implementation, string entityName)> PatchedDeleteMutationEntities { get; } = new Dictionary<Type, (Type implementation, string entityName)>();

        private static readonly MethodInfo AddObjectTypeMethod = typeof(SchemaBuilderExtensions).GetMethods()
            .First(x => x is { Name: nameof(SchemaBuilderExtensions.AddObjectType), ContainsGenericParameters: true } &&
                       x.GetParameters().Length > 1);

        private static readonly List<string> SpecialExtensionFieldNames = new List<string>
        {
            nameof(ObjectTypeExtension.Kind),
            nameof(ObjectTypeExtension.Scope),
            nameof(ObjectTypeExtension.Name),
            nameof(ObjectTypeExtension.Description),
            nameof(ObjectTypeExtension.ContextData)
        }.Select(x => x.ToCamelCase()).ToList();

        public static Dictionary<Type, List<Type>> OneOfConfigsDictionary =>
            OneOfConfigs.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList());

        #endregion

        public override void OnAfterMergeTypeExtensions()
        {
            // Process IHasDeleteMutation types
            var hasDeleteMutationTypes = GeexModule.RootTypes.Where(x => x.IsAssignableTo<IHasDeleteMutation>());
            foreach (var rootType in hasDeleteMutationTypes)
            {
                var runtimeType = rootType;
                var deleteMutationType = runtimeType.GetInterfaces().FirstOrDefault(x => x.Name.StartsWith(
                    $"{nameof(IHasDeleteMutation)}`1"));

                if (deleteMutationType != null)
                {
                    var entityType = deleteMutationType.GenericTypeArguments[0];
                    var entityName = entityType.Name;
                    if (entityName.StartsWith("I") && char.IsUpper(entityName[1]))
                    {
                        entityName = entityName[1..];
                    }
                    PatchedDeleteMutationEntities.TryAdd(deleteMutationType, (runtimeType, entityName));
                }
            }

            base.OnAfterMergeTypeExtensions();
        }

        public override void OnCreateSchemaError(IDescriptorContext context, Exception error)
        {
            base.OnCreateSchemaError(context, error);
        }

        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            switch (definition)
            {
                case InputObjectTypeDefinition inputDef:
                    ProcessInputObjectType(completionContext, inputDef);
                    break;
                case ObjectTypeDefinition objectDef:
                    ProcessObjectType(completionContext, objectDef);
                    break;
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        #region Private Methods

        private void ProcessInputObjectType(ITypeCompletionContext completionContext, InputObjectTypeDefinition definition)
        {
            var runtimeType = definition.RuntimeType;

            ProcessOneOfConfiguration(completionContext, definition, runtimeType);
            ProcessInputFieldDefaults(completionContext, definition, runtimeType);
        }

        private void ProcessOneOfConfiguration(ITypeCompletionContext completionContext,
            InputObjectTypeDefinition definition, Type runtimeType)
        {
            if (!OneOfConfigsDictionary.TryGetValue(runtimeType, out var subTypes))
                return;

            foreach (var subType in subTypes)
            {
                var newField = new InputFieldDefinition(
                    subType.Name.ToCamelCase(),
                    type: completionContext.TypeInspector.GetInputTypeRef(subType));
                definition.Fields.Add(newField);
            }

            definition.AddDirective("oneOf", completionContext.TypeInspector);
        }

        private void ProcessInputFieldDefaults(ITypeCompletionContext completionContext,
            InputObjectTypeDefinition definition, Type runtimeType)
        {
            var ctor = runtimeType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(x => x.GetParameters().Length == 0);
            if (ctor != null)
            {
                var instance = ctor.Invoke([]);

                foreach (var field in definition.Fields)
                {
                    if (field.Property?.GetValue(instance) != field.RuntimeDefaultValue)
                    {
                        field.Type = completionContext.TypeInspector.MarkTypeRefNullable(field.Type);

                        if (field.DefaultValue == null)
                        {
                            var value = field.Property?.GetValue(instance);
                            if (value != null)
                            {
                                field.DefaultValue = Utils.CreateValueNode(value);
                            }
                        }
                    }
                }
            }
            else
            {
                var message = $"Error occurred while creating instance of '{runtimeType}', please setup a parameterless ctor for input type for nullable inference.";
                _logger.LogError(message);
            }
        }

        private void ProcessObjectType(ITypeCompletionContext completionContext, ObjectTypeDefinition definition)
        {
            var runtimeType = definition.RuntimeType;

            if (IsSpecialType(runtimeType))
            {
                IgnoreSpecialExtensionFields(definition);
            }
            else if (runtimeType.IsAssignableTo<IEntityBase>())
            {
                IgnoreEntityMethods(definition);
            }

            // Process delete mutations for Mutation type
            if (typeof(Mutation).IsAssignableFrom(runtimeType))
            {
                ProcessDeleteMutations(completionContext, definition);
            }

            ProcessArgumentDefaults(completionContext, definition);
        }

        private static bool IsSpecialType(Type type)
        {
            return type.Name is nameof(Mutation) or nameof(Query) or nameof(Subscription);
        }

        private void IgnoreSpecialExtensionFields(ObjectTypeDefinition definition)
        {
            var fieldsToIgnore = definition.Fields.Where(x => SpecialExtensionFieldNames.Contains(x.Name));
            foreach (var field in fieldsToIgnore)
            {
                field.Ignore = true;
            }
        }

        private void IgnoreEntityMethods(ObjectTypeDefinition definition)
        {
            //var specialMethods = definition.RuntimeType.GetMethods().Where(x => !x.IsSpecialName);
            //var fieldsToIgnore = definition.Fields.IntersectBy(specialMethods, x => x.Member);

            //foreach (var field in fieldsToIgnore)
            //{
            //    field.Ignore = true;
            //}
        }

        private void ProcessDeleteMutations(ITypeCompletionContext completionContext, ObjectTypeDefinition definition)
        {
            foreach (var (mutationExtType, data) in PatchedDeleteMutationEntities)
            {
                var (implementation, entityName) = data;
                var deleteMethod = mutationExtType?.GetMethod(nameof(IHasDeleteMutation<IEntityBase>.Delete));

                if (deleteMethod != null)
                {
                    var fieldDefinition = new ObjectFieldDefinition($"delete{entityName}",
                        type: TypeReference.Parse("Boolean"),
                        resolver: async (context) =>
                        {
                            var ids = context.ArgumentValue<string[]>("ids");
                            var uow = context.Service<IUnitOfWork>();

                            var instance = context.Service(implementation);
                            return await (deleteMethod.Invoke(instance, [ids, uow]) as Task<bool>);
                        });

                    fieldDefinition.Arguments.Add(new InputFieldDefinition("ids", type: TypeReference.Parse("[String!]!")));

                    definition.Fields.Add(fieldDefinition);
                }
            }
        }

        private void ProcessArgumentDefaults(ITypeCompletionContext completionContext, ObjectTypeDefinition definition)
        {
            foreach (var field in definition.Fields)
            {
                foreach (var argument in field.Arguments)
                {
                    if (argument.Parameter?.HasDefaultValue == true)
                    {
                        argument.Type = completionContext.TypeInspector.MarkTypeRefNullable(argument.Type);

                        if (argument.Parameter.DefaultValue != argument.RuntimeDefaultValue)
                        {
                            argument.DefaultValue = Utils.CreateValueNode(argument.Parameter.DefaultValue);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
