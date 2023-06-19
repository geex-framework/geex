using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstractions;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace Geex.Common.Abstraction.Gql
{
    public class ClassEnumOperationFilterInput<TEnum> : EnumOperationFilterInputType<TEnum>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            base.Configure(descriptor);
        }
    }
}
