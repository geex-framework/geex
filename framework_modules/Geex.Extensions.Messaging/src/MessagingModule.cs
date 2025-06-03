using Volo.Abp.Modularity;

namespace Geex.Extensions.Messaging
{
    [DependsOn(typeof(GeexCoreModule))]
    public class MessagingModule : GeexModule<MessagingModule>
    {
    }
}
