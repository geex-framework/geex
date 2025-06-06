using System;
using HotChocolate.Types;

namespace Geex.Extensions.Messaging.ClientNotification
{
    public abstract class ClientNotify
    {
        protected ClientNotify()
        {
        }
        public class ClientNotifyGqlConfig : GqlConfig.Interface<ClientNotify>
        {
            /// <inheritdoc />
            protected override void Configure(IInterfaceTypeDescriptor<ClientNotify> descriptor)
            {
                base.Configure(descriptor);
            }
        }
        public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.Now;
    }
}
