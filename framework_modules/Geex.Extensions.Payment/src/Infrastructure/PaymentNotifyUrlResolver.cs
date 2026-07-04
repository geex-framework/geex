using Geex.Extensions.Payment.Core.Providers;
using Geex.Extensions.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Payment.Infrastructure;

public class PaymentNotifyUrlResolver
{
    private readonly GeexCoreModuleOptions _coreOptions;
    private readonly PaymentModuleOptions _paymentOptions;
    private readonly ISettingService _settingService;

    public PaymentNotifyUrlResolver(GeexCoreModuleOptions coreOptions, PaymentModuleOptions paymentOptions, ISettingService settingService)
    {
        _coreOptions = coreOptions;
        _paymentOptions = paymentOptions;
        _settingService = settingService;
    }

    public async Task<string> ResolveWeChatNotifyUrlAsync()
    {
        var setting = await _settingService.GetSetting(PaymentSettings.WeChatNotifyUrl, SettingScopeEnumeration.Tenant);
        if (!string.IsNullOrWhiteSpace(setting?.Value?.GetValue<string>()))
            return setting.Value.GetValue<string>()!;
        return BuildUrl(_paymentOptions.WeChatNotifyPath);
    }

    public async Task<string> ResolveAlipayNotifyUrlAsync()
    {
        var setting = await _settingService.GetSetting(PaymentSettings.AlipayNotifyUrl, SettingScopeEnumeration.Tenant);
        if (!string.IsNullOrWhiteSpace(setting?.Value?.GetValue<string>()))
            return setting.Value.GetValue<string>()!;
        return BuildUrl(_paymentOptions.AlipayNotifyPath);
    }

    private string BuildUrl(string path)
        => $"{_coreOptions.Host.TrimEnd('/')}/{path.TrimStart('/')}";
}
