using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Core.Aggregates.Messages;

using HotChocolate.Types;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages.Types
{
    public class MessageGqlType : ObjectType<Message>
    {
        protected override void Configure(IObjectTypeDescriptor<Message> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.ConfigEntity();
            descriptor.Field(x => x.FromUserId);
            descriptor.Field(x => x.MessageType);
            descriptor.Field(x => x.Content);
            descriptor.Field(x => x.ToUserIds);
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.Title);
            descriptor.Field(x => x.Time);
            descriptor.Field(x => x.Severity);
            descriptor.IgnoreMethods();
            descriptor.Implements<InterfaceType<IMessage>>();
            base.Configure(descriptor);
        }
    }
}
