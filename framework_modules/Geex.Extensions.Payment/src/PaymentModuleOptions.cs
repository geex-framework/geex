namespace Geex.Extensions.Payment;

public class PaymentModuleOptions : GeexModuleOption<PaymentModule>
{
    public string WeChatNotifyPath { get; set; } = "/payment/notify/wechat";
    public string AlipayNotifyPath { get; set; } = "/payment/notify/alipay";
    public string DefaultCurrency { get; set; } = "CNY";
    public int PaymentExpireMinutes { get; set; } = 30;
    public bool UseMockProviders { get; set; }
}
