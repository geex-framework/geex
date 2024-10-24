using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Common.Abstraction.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexTypeInterceptor : TypeInterceptor
    {
        public static HashSet<Type> AuditTypes = new HashSet<Type>();
        static MethodInfo AddObjectTypeMethod = typeof(SchemaBuilderExtensions).GetMethods().First(x => x is { Name: nameof(SchemaBuilderExtensions.AddObjectType), ContainsGenericParameters: true } && x.GetParameters().Length > 1);
        public static HashSet<Type> IgnoredTypes { get; } = new HashSet<Type>();
        public static HashSet<KeyValuePair<Type, Type>> OneOfConfigs { get; } = new HashSet<KeyValuePair<Type, Type>>();
        public static Dictionary<Type, List<Type>> OneOfConfigsDictionary => OneOfConfigs.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList());

        /// <inheritdoc />
        public override void OnCreateSchemaError(IDescriptorContext context, Exception error)
        {
            base.OnCreateSchemaError(context, error);
        }

        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            // 只处理InputObjectTypes
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
            base.OnBeforeCompleteType(completionContext, definition);
        }
    }
}
