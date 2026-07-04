namespace Geex.Extensions.Payments;

public class PaymentsModuleOptions : GeexModuleOption<PaymentsModule>
{
    public string ApiDomain { get; set; } = "https://api.shouqianba.com";
    public string ShouqianbaNotifyPath { get; set; } = "/payments/notify/shouqianba";
    public string ShouqianbaRefundNotifyPath { get; set; } = "/payments/notify/shouqianba/refund";
    public string DefaultCurrency { get; set; } = "CNY";
    public int PaymentExpireMinutes { get; set; } = 30;
    public bool UseVirtualTransaction { get; set; }
    public bool VirtualTransactionSimulateCallbacks { get; set; } = true;
    public int VirtualTransactionCallbackDelaySeconds { get; set; } = 1;
}
