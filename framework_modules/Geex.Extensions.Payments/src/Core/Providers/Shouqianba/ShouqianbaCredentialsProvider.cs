using Geex.Extensions.Settings;
using Geex.MultiTenant;

namespace Geex.Extensions.Payments.Core.Providers.Shouqianba;

public class ShouqianbaCredentialsProvider
{
    private readonly ISettingService _settingService;
    private readonly ICurrentTenant _currentTenant;

    public ShouqianbaCredentialsProvider(ISettingService settingService, ICurrentTenant currentTenant)
    {
        _settingService = settingService;
        _currentTenant = currentTenant;
    }

    public async Task<ShouqianbaCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var tenantCode = _currentTenant.Code;
        var terminalSnSetting = await _settingService.GetSetting(PaymentsSettings.TerminalSn, SettingScopeEnumeration.Tenant, tenantCode);
        var terminalKeySetting = await _settingService.GetSetting(PaymentsSettings.TerminalKey, SettingScopeEnumeration.Tenant, tenantCode);

        var terminalSn = terminalSnSetting.Value?.ToString();
        var terminalKey = terminalKeySetting.Value?.ToString();
        if (string.IsNullOrWhiteSpace(terminalSn) || string.IsNullOrWhiteSpace(terminalKey))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Shouqianba terminal credentials are not configured for current tenant.");

        return new ShouqianbaCredentials
        {
            TerminalSn = terminalSn,
            TerminalKey = terminalKey,
        };
    }

    public async Task<ShouqianbaCredentials?> TryGetCredentialsByTerminalSnAsync(string terminalSn, CancellationToken cancellationToken = default)
    {
        var terminalSnSetting = await _settingService.GetSetting(PaymentsSettings.TerminalSn, SettingScopeEnumeration.Tenant, _currentTenant.Code);
        var terminalKeySetting = await _settingService.GetSetting(PaymentsSettings.TerminalKey, SettingScopeEnumeration.Tenant, _currentTenant.Code);
        if (!string.Equals(terminalSnSetting.Value?.ToString(), terminalSn, StringComparison.Ordinal))
            return null;

        var terminalKey = terminalKeySetting.Value?.ToString();
        if (string.IsNullOrWhiteSpace(terminalKey))
            return null;

        return new ShouqianbaCredentials { TerminalSn = terminalSn, TerminalKey = terminalKey };
    }
}
