using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexNamingConventions : DefaultNamingConventions
    {
        public GeexNamingConventions(IDocumentationProvider documentationProvider)
            : base(documentationProvider) { }

        /// <inheritdoc />
        public override string GetTypeName(Type type, TypeKind kind)
        {
            if (kind == TypeKind.InputObject)
            {
                var typeName = base.GetTypeName(type, kind);
                if (typeName.EndsWith("RequestInput", StringComparison.Ordinal))
                {
                    return typeName.Substring(0, typeName.Length - "Input".Length);
                }
            }
            return base.GetTypeName(type, kind);
        }

        ///// <inheritdoc />
        //public override string GetMemberName(MemberInfo member, MemberKind kind)
        //{
        //    var result = base.GetMemberName(member, kind);
        //    if (result.StartsWith("x_Aggregate_x"))
        //    {
        //        result = result.Replace("x_Aggregate_x","x_aggregate_x");
        //    }
        //    return result;
        //}

    }
}
