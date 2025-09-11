using System;
using System.Reflection;
using System.Text.RegularExpressions;

using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Gql
{
    public class GeexNamingConventions : DefaultNamingConventions
    {
        private static readonly Regex NonAsciiRegex = new Regex("[\u4e00-\u9fa5]", RegexOptions.Compiled);
        public GeexNamingConventions(IDocumentationProvider documentationProvider)
            : base(documentationProvider) { }

        /// <inheritdoc />
        public override string GetTypeName(Type type, TypeKind kind)
        {
            if (NonAsciiRegex.IsMatch(type.Name))
            {
                return type.Name;
            }
            var typeName = base.GetTypeName(type, kind);
            if (kind == TypeKind.InputObject && typeName.EndsWith("RequestInput", StringComparison.Ordinal))
            {
                return typeName.Substring(0, typeName.Length - 5);
            }
            return typeName;
        }

        /// <inheritdoc />
        public override string GetMemberName(MemberInfo member, MemberKind kind)
        {
            if (NonAsciiRegex.IsMatch(member.Name))
            {
                return member.Name;
            }
            return base.GetMemberName(member, kind);
        }

        /// <inheritdoc />
        public override string GetArgumentName(ParameterInfo parameter)
        {
            if (NonAsciiRegex.IsMatch(parameter.Name))
            {
                return parameter.Name;
            }
            return base.GetArgumentName(parameter);
        }

        /// <inheritdoc />
        public override string GetEnumValueName(object value)
        {
            var valueStr = value.ToString();
            if (NonAsciiRegex.IsMatch(valueStr))
            {
                return valueStr;
            }
            return base.GetEnumValueName(value);
        }

        /// <inheritdoc />
        public override string GetTypeName(Type type)
        {
            if (NonAsciiRegex.IsMatch(type.Name))
            {
                return type.Name;
            }
            return base.GetTypeName(type);
        }
    }
}
