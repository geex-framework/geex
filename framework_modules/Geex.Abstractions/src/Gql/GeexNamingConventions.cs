using System;
using System.Reflection;

using GreenDonut;

using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Abstractions.Gql
{
    public class GeexNamingConventions : DefaultNamingConventions
    {
        public GeexNamingConventions(IDocumentationProvider documentationProvider)
            : base(documentationProvider) { }

        /// <inheritdoc />
        public override string GetTypeName(Type type, TypeKind kind)
        {
            var typeName = base.GetTypeName(type, kind);
            if (kind == TypeKind.InputObject && typeName.EndsWith("RequestInput", StringComparison.Ordinal))
            {
                return typeName.Substring(0, typeName.Length - 5);
            }
            return typeName;
        }
    }
}
