using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Enumerations;
using Geex.Common.Gql.Types;

using HotChocolate;
using HotChocolate.Types;

namespace Geex.Common.Messaging.Api.Aggregates.FrontendCalls
{
    public interface IFrontendCall
    {
        public FrontendCallType FrontendCallType { get; }
        public JsonNode? Data { get; }
    }

    public class IFrontendCallGqlType : GqlConfig.Interface<IFrontendCall>
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
