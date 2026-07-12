using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Settings;
using Geex.MultiTenant;

namespace Geex.Extensions.Messaging.Core.Sms;

public class TencentCloudSmsCredentials
{
    public string SecretId { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string SdkAppId { get; init; } = string.Empty;
    public string SignName { get; init; } = string.Empty;
    public string TemplateId { get; init; } = string.Empty;
}

public class TencentCloudSmsCredentialsProvider
{
    private readonly ISettingService _settingService;
    private readonly ICurrentTenant _currentTenant;

    public TencentCloudSmsCredentialsProvider(ISettingService settingService, ICurrentTenant currentTenant)
    {
        _settingService = settingService;
        _currentTenant = currentTenant;
    }

    public async Task<TencentCloudSmsCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var tenantCode = _currentTenant.Code;
        var secretId = (await _settingService.GetSetting(MessagingSettings.SmsSecretId, SettingScopeEnumeration.Tenant, tenantCode)).Value?.ToString();
        var secretKey = (await _settingService.GetSetting(MessagingSettings.SmsSecretKey, SettingScopeEnumeration.Tenant, tenantCode)).Value?.ToString();
        var sdkAppId = (await _settingService.GetSetting(MessagingSettings.SmsSdkAppId, SettingScopeEnumeration.Tenant, tenantCode)).Value?.ToString();
        var signName = (await _settingService.GetSetting(MessagingSettings.SmsSignName, SettingScopeEnumeration.Tenant, tenantCode)).Value?.ToString();
        var templateId = (await _settingService.GetSetting(MessagingSettings.SmsTemplateId, SettingScopeEnumeration.Tenant, tenantCode)).Value?.ToString();

        if (string.IsNullOrWhiteSpace(secretId) || string.IsNullOrWhiteSpace(secretKey) ||
            string.IsNullOrWhiteSpace(sdkAppId) || string.IsNullOrWhiteSpace(signName) ||
            string.IsNullOrWhiteSpace(templateId))
        {
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "腾讯云 SMS 凭证未为当前租户配置完整");
        }

        return new TencentCloudSmsCredentials
        {
            SecretId = secretId,
            SecretKey = secretKey,
            SdkAppId = sdkAppId,
            SignName = signName,
            TemplateId = templateId,
        };
    }
}
