using Geex.Extensions.Payment.Infrastructure;
using Geex.Extensions.Settings;
using Microsoft.AspNetCore.Http;
using SKIT.FlurlHttpClient.Wechat.TenpayV3;
using SKIT.FlurlHttpClient.Wechat.TenpayV3.Models;

namespace Geex.Extensions.Payment.Core.Providers;

public class WeChatPayProvider : IPaymentProvider
{
    private readonly ISettingService _settingService;
    private readonly PaymentNotifyUrlResolver _notifyUrlResolver;

    public WeChatPayProvider(ISettingService settingService, PaymentNotifyUrlResolver notifyUrlResolver)
    {
        _settingService = settingService;
        _notifyUrlResolver = notifyUrlResolver;
    }

    public PaymentProviderEnum Provider => PaymentProviderEnum.WeChatPay;

    public async Task<PaymentPrepayResult> CreatePaymentAsync(IPaymentOrder order, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default)
    {
        if (channel == PaymentChannelEnum.PcWeb)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "WeChat Pay does not support PcWeb channel. Use Native instead.");

        var client = await CreateClientAsync();
        var notifyUrl = await _notifyUrlResolver.ResolveWeChatNotifyUrlAsync();
        var amount = (int)(order.Amount * 100);

        if (channel == PaymentChannelEnum.Native)
        {
            var request = new CreatePayTransactionNativeRequest
            {
                OutTradeNumber = order.OutTradeNo,
                Description = order.Subject,
                NotifyUrl = notifyUrl,
                Amount = new CreatePayTransactionNativeRequest.Types.Amount { Total = amount, Currency = order.Currency },
            };
            var response = await client.ExecuteCreatePayTransactionNativeAsync(request, cancellationToken);
            if (!response.IsSuccessful())
                throw new BusinessException(GeexExceptionType.OnPurpose, message: response.ErrorMessage ?? "WeChat native pay failed.");
            return new PaymentPrepayResult { OutTradeNo = order.OutTradeNo, CodeUrl = response.QrcodeUrl };
        }

        if (string.IsNullOrWhiteSpace(context.OpenId))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "OpenId is required for WeChat JSAPI payment.");

        var jsapiRequest = new CreatePayTransactionJsapiRequest
        {
            OutTradeNumber = order.OutTradeNo,
            Description = order.Subject,
            NotifyUrl = notifyUrl,
            Amount = new CreatePayTransactionJsapiRequest.Types.Amount { Total = amount, Currency = order.Currency },
            Payer = new CreatePayTransactionJsapiRequest.Types.Payer { OpenId = context.OpenId },
        };
        var jsapiResponse = await client.ExecuteCreatePayTransactionJsapiAsync(jsapiRequest, cancellationToken);
        if (!jsapiResponse.IsSuccessful())
            throw new BusinessException(GeexExceptionType.OnPurpose, message: jsapiResponse.ErrorMessage ?? "WeChat jsapi pay failed.");

        var appId = await GetSettingValueAsync(PaymentSettings.WeChatAppId);
        var jsapiParams = client.GenerateParametersForJsapiPayRequest(appId, jsapiResponse.PrepayId);
        return new PaymentPrepayResult
        {
            OutTradeNo = order.OutTradeNo,
            PrepayId = jsapiResponse.PrepayId,
            JsApiParams = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(jsapiParams)),
        };
    }

    public async Task<PaymentQueryResult> QueryPaymentAsync(IPaymentOrder order, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync();
        var response = await client.ExecuteGetPayTransactionByOutTradeNumberAsync(new GetPayTransactionByOutTradeNumberRequest
        {
            OutTradeNumber = order.OutTradeNo,
        }, cancellationToken);
        if (!response.IsSuccessful())
            return new PaymentQueryResult { Status = order.Status };
        var status = response.TradeState switch
        {
            "SUCCESS" => PaymentStatusEnum.Succeeded,
            "CLOSED" => PaymentStatusEnum.Closed,
            "NOTPAY" => PaymentStatusEnum.Pending,
            _ => order.Status,
        };
        return new PaymentQueryResult { Status = status, TransactionId = response.TransactionId };
    }

    public Task<PaymentCallbackResult> HandleCallbackAsync(HttpRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("WeChat callback is handled by the payment notify endpoint.");

    private async Task<WechatTenpayClient> CreateClientAsync()
    {
        var mchId = await GetSettingValueAsync(PaymentSettings.WeChatMchId);
        var appId = await GetSettingValueAsync(PaymentSettings.WeChatAppId);
        var apiV3Key = await GetSettingValueAsync(PaymentSettings.WeChatApiV3Key);
        var serial = await GetSettingValueAsync(PaymentSettings.WeChatCertificateSerialNumber);
        var privateKey = await GetSettingValueAsync(PaymentSettings.WeChatCertificatePrivateKey);
        if (string.IsNullOrWhiteSpace(mchId) || string.IsNullOrWhiteSpace(privateKey))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "WeChat Pay tenant settings are not configured.");

        return new WechatTenpayClient(new WechatTenpayClientOptions
        {
            MerchantId = mchId,
            MerchantV3Secret = apiV3Key,
            MerchantCertificateSerialNumber = serial,
            MerchantCertificatePrivateKey = privateKey,
        });
    }

    private async Task<string> GetSettingValueAsync(PaymentSettings setting)
    {
        var value = await _settingService.GetSetting(setting, SettingScopeEnumeration.Tenant);
        return value?.Value?.GetValue<string>() ?? string.Empty;
    }
}
