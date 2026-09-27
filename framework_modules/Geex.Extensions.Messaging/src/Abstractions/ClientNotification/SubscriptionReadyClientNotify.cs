using HotChocolate.Types;
namespace Geex.Extensions.Messaging.ClientNotification;

public sealed class SubscriptionReadyClientNotify : ClientNotify
{
    public class SubscriptionReadyClientNotifyGqlConfig : GqlConfig.Object<SubscriptionReadyClientNotify>
    {
        protected override void Configure(IObjectTypeDescriptor<SubscriptionReadyClientNotify> descriptor)
        {
            descriptor.Implements<ClientNotifyGqlConfig>();
            base.Configure(descriptor);
        }
    }
}
