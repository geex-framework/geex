using System;
using System.Reflection;
using HotChocolate;
using HotChocolate.Data.Filters;
using HotChocolate.Types.Descriptors;

namespace Geex.Common
{
    internal class GeexFilterConvention : FilterConvention
    {
        /// <inheritdoc />
        public override string GetFieldName(MemberInfo member)
        {
            return base.GetFieldName(member);
        }
    }
}