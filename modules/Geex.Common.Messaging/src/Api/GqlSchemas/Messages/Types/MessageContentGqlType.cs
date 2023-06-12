using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using HotChocolate.Types;
using KuanFang.Rms.MessageManagement.Messages;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages.Types
{
    public class MessageContentGqlType : ObjectType<IMessageContent>
    {
        protected override void Configure(IObjectTypeDescriptor<IMessageContent> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            base.Configure(descriptor);
        }
    }
}
