using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexNamingConventions : DefaultNamingConventions
    {
        public GeexNamingConventions(IDocumentationProvider documentationProvider)
            : base(documentationProvider) { }

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
