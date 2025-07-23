using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Gql.Extensions;
using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Entities;

namespace Geex.Gql
{
    public class GeexTypeInterceptor(ILogger<GeexTypeInterceptor> _logger) : TypeInterceptor
    {
        #region Static Configuration

        public static HashSet<Type> AuditTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> IgnoredTypes { get; } = new HashSet<Type>();
        public static HashSet<KeyValuePair<Type, Type>> OneOfConfigs { get; } = new HashSet<KeyValuePair<Type, Type>>();

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
            try
            {
                var instance = Activator.CreateInstance(runtimeType, nonPublic: true);

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
            catch (Exception e)
            {
                var message = $"Error occurred while creating instance of {runtimeType} when marking nullable fields";
                _logger.LogError(e, message);
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
            var specialMethods = definition.RuntimeType.GetMethods().Where(x => !x.IsSpecialName);
            var fieldsToIgnore = definition.Fields.IntersectBy(specialMethods, x => x.Member);

            foreach (var field in fieldsToIgnore)
            {
                field.Ignore = true;
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
