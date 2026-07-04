namespace Geex.Extensions.Payments.Infrastructure;

public class PaymentNotifyUrlResolver
{
    private readonly GeexCoreModuleOptions _coreOptions;
    private readonly PaymentsModuleOptions _paymentsOptions;

    public PaymentNotifyUrlResolver(GeexCoreModuleOptions coreOptions, PaymentsModuleOptions paymentsOptions)
    {
        _coreOptions = coreOptions;
        _paymentsOptions = paymentsOptions;
    }

    public string GetPaymentNotifyUrl()
        => $"{_coreOptions.Host.TrimEnd('/')}{_paymentsOptions.ShouqianbaNotifyPath}";

    public string GetRefundNotifyUrl()
        => $"{_coreOptions.Host.TrimEnd('/')}{_paymentsOptions.ShouqianbaRefundNotifyPath}";
}
