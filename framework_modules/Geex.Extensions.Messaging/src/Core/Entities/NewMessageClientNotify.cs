using Geex.ClientNotification;
using HotChocolate.Types;

namespace Geex.Extensions.Messaging.Core.Entities
{
    public class NewMessageClientNotify : ClientNotify
    {
        public Message Message { get; }

        /// <param name="message"></param>
        /// <inheritdoc />
        public NewMessageClientNotify(Message message)
        {
            Message = message;
        }
        public class NewMessageClientNotifyGqlConfig : GqlConfig.Object<NewMessageClientNotify>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<NewMessageClientNotify> descriptor)
            {
                descriptor.Implements<ClientNotifyGqlConfig>();
                base.Configure(descriptor);
            }
        }
    }
}
