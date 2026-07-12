using Geex.Extensions.Settings;

namespace Geex.Extensions.Messaging
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
        public static MessagingSettings SmsSecretId { get; } = new(nameof(SmsSecretId), new[] { SettingScopeEnumeration.Tenant }, "腾讯云 SMS SecretId", isHiddenForClients: true);
        public static MessagingSettings SmsSecretKey { get; } = new(nameof(SmsSecretKey), new[] { SettingScopeEnumeration.Tenant }, "腾讯云 SMS SecretKey", isHiddenForClients: true);
        public static MessagingSettings SmsSdkAppId { get; } = new(nameof(SmsSdkAppId), new[] { SettingScopeEnumeration.Tenant }, "腾讯云 SMS SdkAppId");
        public static MessagingSettings SmsSignName { get; } = new(nameof(SmsSignName), new[] { SettingScopeEnumeration.Tenant }, "腾讯云 SMS 签名");
        public static MessagingSettings SmsTemplateId { get; } = new(nameof(SmsTemplateId), new[] { SettingScopeEnumeration.Tenant }, "腾讯云 SMS 模板 ID");
        public static MessagingSettings SmsProvider { get; } = new(nameof(SmsProvider), new[] { SettingScopeEnumeration.Tenant }, "SMS 提供商");
    }
}
