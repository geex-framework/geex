using Geex.Common.Settings.Abstraction;

namespace _org_._proj_._mod_.Api
{
    public class _mod_Settings : SettingDefinition
    {
        public _mod_Settings(string name,
            SettingScopeEnumeration[] validScopes = default,
            string? description = null,
            bool isHiddenForClients = false) : base(nameof(_mod_) + name, validScopes, description, isHiddenForClients)
        {
        }
        public static _mod_Settings ModuleName { get; } = new(nameof(ModuleName), new[] { SettingScopeEnumeration.Global, }, "_mod_");

    }
}
