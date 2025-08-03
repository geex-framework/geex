using Geex.Extensions.Settings;

namespace Geex.Extensions.BlobStorage
{
    public class BlobStorageSettings : SettingDefinition
    {
        public BlobStorageSettings(string name,
            SettingScopeEnumeration[] validScopes = default,
            string? description = null,
            bool isHiddenForClients = false) : base(nameof(BlobStorage) + name, validScopes, description, isHiddenForClients)
        {
        }
        public static BlobStorageSettings ModuleName { get; } = new(nameof(ModuleName), new[] { SettingScopeEnumeration.Global, }, "BlobStorage");

    }
}
