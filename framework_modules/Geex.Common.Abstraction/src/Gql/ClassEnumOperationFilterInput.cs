using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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
