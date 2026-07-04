using Aop.Api;
using Aop.Api.Request;
using Aop.Api.Response;
using Geex.Extensions.Payment.Infrastructure;
using Geex.Extensions.Settings;
using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Payment.Core.Providers;

public class AlipayProvider : IPaymentProvider
{
    private const string Gateway = "https://openapi.alipay.com/gateway.do";
    private readonly ISettingService _settingService;
    private readonly PaymentNotifyUrlResolver _notifyUrlResolver;

    public AlipayProvider(ISettingService settingService, PaymentNotifyUrlResolver notifyUrlResolver)
    {
        _settingService = settingService;
        _notifyUrlResolver = notifyUrlResolver;
    }

    public PaymentProviderEnum Provider => PaymentProviderEnum.Alipay;

    public async Task<PaymentPrepayResult> CreatePaymentAsync(IPaymentOrder order, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync();
        var notifyUrl = await _notifyUrlResolver.ResolveAlipayNotifyUrlAsync();

        if (channel == PaymentChannelEnum.Native)
            return CreatePrecreate(client, order, notifyUrl);

        if (channel == PaymentChannelEnum.JsApi)
            return CreateTrade(client, order, notifyUrl, context.BuyerId);

        if (channel == PaymentChannelEnum.PcWeb)
            return CreatePagePay(client, order, notifyUrl);

        throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Unsupported Alipay channel: {channel.Name}.");
    }

    public Task<PaymentQueryResult> QueryPaymentAsync(IPaymentOrder order, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentQueryResult { Status = order.Status, TransactionId = order.TransactionId });

    public Task<PaymentCallbackResult> HandleCallbackAsync(HttpRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Alipay callback is handled by the payment notify endpoint.");

    private static PaymentPrepayResult CreatePrecreate(IAopClient client, IPaymentOrder order, string notifyUrl)
    {
        var request = new AlipayTradePrecreateRequest
        {
            BizContent = System.Text.Json.JsonSerializer.Serialize(new
            {
                out_trade_no = order.OutTradeNo,
                total_amount = order.Amount.ToString("0.##"),
                subject = order.Subject,
            }),
        };
        request.SetNotifyUrl(notifyUrl);
        var response = client.Execute(request) as AlipayTradePrecreateResponse
            ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: "Alipay precreate failed.");
        if (!response.IsError)
            return new PaymentPrepayResult { OutTradeNo = order.OutTradeNo, CodeUrl = response.QrCode };
        throw new BusinessException(GeexExceptionType.OnPurpose, message: response.SubMsg ?? response.Msg ?? "Alipay precreate failed.");
    }

    private static PaymentPrepayResult CreateTrade(IAopClient client, IPaymentOrder order, string notifyUrl, string? buyerId)
    {
        if (string.IsNullOrWhiteSpace(buyerId))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "BuyerId is required for Alipay JSAPI payment.");

        var request = new AlipayTradeCreateRequest
        {
            BizContent = System.Text.Json.JsonSerializer.Serialize(new
            {
                out_trade_no = order.OutTradeNo,
                total_amount = order.Amount.ToString("0.##"),
                subject = order.Subject,
                buyer_id = buyerId,
            }),
        };
        request.SetNotifyUrl(notifyUrl);
        var response = client.Execute(request) as AlipayTradeCreateResponse
            ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: "Alipay trade create failed.");
        if (!response.IsError)
            return new PaymentPrepayResult { OutTradeNo = order.OutTradeNo, PrepayId = response.TradeNo };
        throw new BusinessException(GeexExceptionType.OnPurpose, message: response.SubMsg ?? response.Msg ?? "Alipay trade create failed.");
    }

    private static PaymentPrepayResult CreatePagePay(IAopClient client, IPaymentOrder order, string notifyUrl)
    {
        var request = new AlipayTradePagePayRequest
        {
            BizContent = System.Text.Json.JsonSerializer.Serialize(new
            {
                out_trade_no = order.OutTradeNo,
                product_code = "FAST_INSTANT_TRADE_PAY",
                total_amount = order.Amount.ToString("0.##"),
                subject = order.Subject,
            }),
        };
        request.SetNotifyUrl(notifyUrl);
        var form = client.pageExecute(request);
        return new PaymentPrepayResult { OutTradeNo = order.OutTradeNo, PagePayForm = form.Body };
    }

    private async Task<IAopClient> CreateClientAsync()
    {
        var appId = await GetSettingValueAsync(PaymentSettings.AlipayAppId);
        var privateKey = await GetSettingValueAsync(PaymentSettings.AlipayPrivateKey);
        var publicKey = await GetSettingValueAsync(PaymentSettings.AlipayPublicKey);
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(privateKey))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Alipay tenant settings are not configured.");
        return new DefaultAopClient(Gateway, appId, privateKey, "json", "1.0", "RSA2", publicKey, "utf-8", false);
    }

    private async Task<string> GetSettingValueAsync(PaymentSettings setting)
    {
        var value = await _settingService.GetSetting(setting, SettingScopeEnumeration.Tenant);
        return value?.Value?.GetValue<string>() ?? string.Empty;
    }
}
