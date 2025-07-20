using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql.Extensions
{
    public static class FilterFieldDescriptorExtensions
    {
        public static void MakeNullable(this IInputFieldDescriptor descriptor) =>
            descriptor.Extend()
                .OnBeforeCreate(
                    (c, def) => def.Type = RewriteTypeToNullableType(def, c.TypeInspector));

        public static void MakeNullable(this IObjectFieldDescriptor descriptor) =>
            descriptor.Extend()
                .OnBeforeCreate(
                    (c, def) => def.Type = RewriteTypeToNullableType(def, c.TypeInspector));

        public static void MakeNullable(this IInterfaceFieldDescriptor descriptor) =>
            descriptor.Extend()
                .OnBeforeCreate(
                    (c, def) => def.Type = RewriteTypeToNullableType(def, c.TypeInspector));

        public static void MakeNullable(this IArgumentDescriptor descriptor) =>
            descriptor.Extend()
                .OnBeforeCreate(
                    (c, def) => def.Type = RewriteTypeToNullableType(def, c.TypeInspector));

        private static TypeReference RewriteTypeToNullableType(
            InputFieldDefinition configuration,
            ITypeInspector typeInspector)
        {
            var reference = configuration.Type;
            return MarkTypeRefNullable(typeInspector, reference);
        }

        private static TypeReference RewriteTypeToNullableType(
            ObjectFieldDefinition configuration,
            ITypeInspector typeInspector)
        {
            var reference = configuration.Type;

            return MarkTypeRefNullable(typeInspector, reference);
        }

        private static TypeReference RewriteTypeToNullableType(
            ArgumentDefinition configuration,
            ITypeInspector typeInspector)
        {
            var reference = configuration.Type;

            return MarkTypeRefNullable(typeInspector, reference);
        }

        private static TypeReference RewriteTypeToNullableType(
            InterfaceFieldDefinition configuration,
            ITypeInspector typeInspector)
        {
            var reference = configuration.Type;

            return MarkTypeRefNullable(typeInspector, reference);
        }

        private static TypeReference MarkTypeRefNullable(ITypeInspector typeInspector, TypeReference? reference)
        {
            if (reference is ExtendedTypeReference extendedTypeRef)
            {
                return extendedTypeRef.Type.IsNullable
                    ? extendedTypeRef
                    : extendedTypeRef.WithType(
                        typeInspector.ChangeNullability(extendedTypeRef.Type, true));
            }

            if (reference is SchemaTypeReference schemaRef)
            {
                return schemaRef.Type is NonNullType nnt
                    ? schemaRef.WithType(nnt.NullableType())
                    : schemaRef;
            }

            if (reference is SyntaxTypeReference syntaxRef)
            {
                return syntaxRef.Type is NonNullTypeNode nnt
                    ? syntaxRef.WithType(nnt.Type)
                    : syntaxRef;
            }

            throw new NotSupportedException();
        }
    }
}
