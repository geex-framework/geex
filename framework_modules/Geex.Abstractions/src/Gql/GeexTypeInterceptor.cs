using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

using MongoDB.Entities;

namespace Geex.Gql
{
    public class GeexTypeInterceptor : TypeInterceptor
    {
        public static HashSet<Type> AuditTypes = new HashSet<Type>();

        static MethodInfo AddObjectTypeMethod = typeof(SchemaBuilderExtensions).GetMethods().First(x =>
            x is { Name: nameof(SchemaBuilderExtensions.AddObjectType), ContainsGenericParameters: true } &&
            x.GetParameters().Length > 1);

        public static HashSet<Type> IgnoredTypes { get; } = new HashSet<Type>();
        public static HashSet<KeyValuePair<Type, Type>> OneOfConfigs { get; } = new HashSet<KeyValuePair<Type, Type>>();

        public static Dictionary<Type, List<Type>> OneOfConfigsDictionary => OneOfConfigs.GroupBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList());

        /// <inheritdoc />
        public override void OnCreateSchemaError(IDescriptorContext context, Exception error)
        {
            base.OnCreateSchemaError(context, error);
        }

        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is InputObjectTypeDefinition inputObjectTypeDefinition)
            {
                if (OneOfConfigsDictionary.TryGetValue(inputObjectTypeDefinition.RuntimeType, out var subTypes))
                {
                    foreach (var subType in subTypes)
                    {
                        // 在现有字段上增加一个新的字段
                        var newField = new InputFieldDefinition(
                            subType.Name.ToCamelCase(),
                            type: completionContext.TypeInspector.GetInputTypeRef(subType));
                        inputObjectTypeDefinition.Fields.Add(newField);
                    }

                    inputObjectTypeDefinition.AddDirective("oneOf", completionContext.TypeInspector);
                }
            }
            else if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var specialExtensionFieldNames = new List<string>
                {
                    nameof(ObjectTypeExtension.Kind),
                    nameof(ObjectTypeExtension.Scope),
                    nameof(ObjectTypeExtension.Name),
                    nameof(ObjectTypeExtension.Description),
                    nameof(ObjectTypeExtension.ContextData)
                }.Select(x => x.ToCamelCase()).ToList();
                if (objectTypeDefinition.RuntimeType.Name is nameof(Mutation) or nameof(Query) or nameof(Subscription))
                {
                    var specialFieldDefinitions =
                        objectTypeDefinition.Fields.Where(x => specialExtensionFieldNames.Contains(x.Name));
                    foreach (var specialFieldDefinition in specialFieldDefinitions)
                    {
                        specialFieldDefinition.Ignore = true;
                    }
                }
                else if (objectTypeDefinition.RuntimeType.IsAssignableTo<IEntityBase>())
                {
                    var specialMethods = objectTypeDefinition.RuntimeType.GetMethods().Where(x => !x.IsSpecialName);
                    var specialFieldDefinitions =
                        objectTypeDefinition.Fields.IntersectBy(specialMethods, x => x.Member);
                    foreach (var fieldDefinition in specialFieldDefinitions)
                    {
                        fieldDefinition.Ignore = true;
                    }
                }

                base.OnBeforeCompleteType(completionContext, definition);
            }
        }
    }
}
