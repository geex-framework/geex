using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Core.Aggregates.FrontendCalls;

using HotChocolate.Types;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages.Types
{
    public class FrontendCallGqlType : ObjectType<FrontendCall>
    {
        protected override void Configure(IObjectTypeDescriptor<FrontendCall> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.Field(x => x.Data);
            descriptor.Implements<InterfaceType<IFrontendCall>>();
            base.Configure(descriptor);
        }
    }

    public class IFrontendCallGqlType : InterfaceType<IFrontendCall>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IFrontendCall> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.Field(x => x.Data);
            base.Configure(descriptor);
        }
    }
}
