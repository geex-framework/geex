using Geex.Extensions.Settings;

namespace Geex.Extensions.Payments;

public class PaymentsSettings : SettingDefinition
{
    public PaymentsSettings(string name,
        SettingScopeEnumeration[]? validScopes = default,
        string? description = null,
        bool isHiddenForClients = false) : base(nameof(Payments) + name, validScopes, description, isHiddenForClients)
    {
    }

    public static PaymentsSettings TerminalSn { get; } = new(nameof(TerminalSn), new[] { SettingScopeEnumeration.Tenant }, "收钱吧终端号");
    public static PaymentsSettings TerminalKey { get; } = new(nameof(TerminalKey), new[] { SettingScopeEnumeration.Tenant }, "收钱吧终端密钥", isHiddenForClients: true);
    public static PaymentsSettings VendorSn { get; } = new(nameof(VendorSn), new[] { SettingScopeEnumeration.Tenant }, "收钱吧服务商序列号");
    public static PaymentsSettings VendorKey { get; } = new(nameof(VendorKey), new[] { SettingScopeEnumeration.Tenant }, "收钱吧服务商密钥", isHiddenForClients: true);
}
