using Geex.Abstractions;
using Geex.Extensions.Settings;

namespace Geex.Extensions.Messaging.Api
{
    public class MessagingSettings : SettingDefinition
    {
        public MessagingSettings(string name,
            SettingScopeEnumeration[] validScopes = default,
            string? description = null,
            bool isHiddenForClients = false) : base(nameof(Messaging) + name, validScopes, description, isHiddenForClients)
        {
        }
        public static MessagingSettings ModuleName { get; } = new(nameof(ModuleName), new[] { SettingScopeEnumeration.Global, });

    }
}
