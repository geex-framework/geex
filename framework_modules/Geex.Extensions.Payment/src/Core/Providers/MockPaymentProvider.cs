using System.Text.Json;
using System.Text.Json.Nodes;
using Geex.Extensions.Payment.Core.Handlers;
using Geex.Extensions.Payment.Requests;
using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Payment.Core.Providers;

public class MockPaymentProvider : IPaymentProvider
{
    private readonly IUnitOfWork _uow;

    public MockPaymentProvider(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public PaymentProviderEnum Provider => PaymentProviderEnum.Mock;

    public Task<PaymentPrepayResult> CreatePaymentAsync(IPaymentOrder order, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default)
    {
        var result = new PaymentPrepayResult
        {
            OutTradeNo = order.OutTradeNo,
            PrepayId = $"mock-prepay-{order.OutTradeNo}",
        };

        if (channel == PaymentChannelEnum.Native)
            result.CodeUrl = $"mock://pay/native/{order.OutTradeNo}";
        else if (channel == PaymentChannelEnum.JsApi)
            result.JsApiParams = JsonNode.Parse($"{{\"appId\":\"mock\",\"package\":\"prepay_id={result.PrepayId}\"}}");
        else if (channel == PaymentChannelEnum.PcWeb)
            result.PagePayForm = $"<form mock-pay=\"{order.OutTradeNo}\"></form>";

        return Task.FromResult(result);
    }

    public Task<PaymentQueryResult> QueryPaymentAsync(IPaymentOrder order, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentQueryResult
        {
            Status = order.Status,
            TransactionId = order.TransactionId,
        });

    public async Task<PaymentCallbackResult> HandleCallbackAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<MockCallbackPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload?.OutTradeNo is null)
        {
            return new PaymentCallbackResult
            {
                Success = false,
                ResponseBody = """{"code":"FAIL","message":"invalid payload"}""",
            };
        }

        await _uow.Request(new CompletePaymentRequest(payload.OutTradeNo, payload.TransactionId ?? $"mock-tx-{payload.OutTradeNo}"));
        await _uow.SaveChanges();

        return new PaymentCallbackResult
        {
            Success = true,
            OutTradeNo = payload.OutTradeNo,
            TransactionId = payload.TransactionId,
            ResponseBody = """{"code":"SUCCESS","message":"success"}""",
        };
    }

    private sealed class MockCallbackPayload
    {
        public string? OutTradeNo { get; set; }
        public string? TransactionId { get; set; }
    }
}
