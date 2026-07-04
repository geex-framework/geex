using Geex.Extensions.Settings;

namespace Geex.Extensions.Payment;

public class PaymentSettings : SettingDefinition
{
    public PaymentSettings(string name, SettingScopeEnumeration[]? validScopes = null, string? description = null, bool isHiddenForClients = true)
        : base(nameof(Payment) + name, validScopes ?? [SettingScopeEnumeration.Tenant], description, isHiddenForClients)
    {
    }

    public static PaymentSettings WeChatMchId { get; } = new(nameof(WeChatMchId), description: "微信商户号");
    public static PaymentSettings WeChatAppId { get; } = new(nameof(WeChatAppId), description: "微信 AppId");
    public static PaymentSettings WeChatApiV3Key { get; } = new(nameof(WeChatApiV3Key), description: "微信 APIv3 密钥");
    public static PaymentSettings WeChatCertificateSerialNumber { get; } = new(nameof(WeChatCertificateSerialNumber), description: "微信证书序列号");
    public static PaymentSettings WeChatCertificatePrivateKey { get; } = new(nameof(WeChatCertificatePrivateKey), description: "微信商户私钥 PEM");
    public static PaymentSettings WeChatNotifyUrl { get; } = new(nameof(WeChatNotifyUrl), description: "微信支付回调地址");
    public static PaymentSettings AlipayAppId { get; } = new(nameof(AlipayAppId), description: "支付宝 AppId");
    public static PaymentSettings AlipayPrivateKey { get; } = new(nameof(AlipayPrivateKey), description: "支付宝应用私钥");
    public static PaymentSettings AlipayPublicKey { get; } = new(nameof(AlipayPublicKey), description: "支付宝公钥");
    public static PaymentSettings AlipayNotifyUrl { get; } = new(nameof(AlipayNotifyUrl), description: "支付宝回调地址");
}
