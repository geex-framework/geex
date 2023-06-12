using System.Text.Json.Nodes;
using Geex.Common.Settings.Abstraction;

namespace Geex.Bms.Demo.Core
{
    public class DemoSettings : SettingDefinition
    {
        public DemoSettings(string name,
            SettingScopeEnumeration[] validScopes = default,
            string description = null,
            JsonNode defaultValue = null,
            bool isHiddenForClients = false) : base(nameof(Demo) + name, validScopes, description, isHiddenForClients, defaultValue)
        {
        }
        public static DemoSettings ModuleName { get; } = new(nameof(ModuleName), new[] { SettingScopeEnumeration.Global, }, "name of this module", "Demo");


        public static DemoSettings MaxBorrowingQtySettings { get; } = new(nameof(MaxBorrowingQtySettings), new[] { SettingScopeEnumeration.Global, }, "MaxBorrowingQtySettings", JsonValue.Create(3));
    }
}
