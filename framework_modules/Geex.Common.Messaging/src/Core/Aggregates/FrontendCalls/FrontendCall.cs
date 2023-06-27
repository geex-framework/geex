using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;

using HotChocolate.Types;

namespace Geex.Common.Messaging.Core.Aggregates.FrontendCalls
{
    public class FrontendCall : IFrontendCall
    {
        public FrontendCall(FrontendCallType frontendCallType, JsonNode? data)
        {
            FrontendCallType = frontendCallType;
            Data = data;
        }

        public FrontendCallType FrontendCallType { get; }
        public JsonNode? Data { get; }

        public class FrontendCallGqlType : GqlConfig.Object<FrontendCall>
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
    }

}
