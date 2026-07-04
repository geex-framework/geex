using Geex.Extensions.Payments.Core.Tasks;
using Geex.Extensions.Payments.Infrastructure;
using Geex.Extensions.Payments.Requests;
using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Payments.Core.Providers;

public class VirtualTransactionPaymentProvider : IPaymentProvider
{
    private readonly IUnitOfWork _uow;
    private readonly PaymentsModuleOptions _options;

    public VirtualTransactionPaymentProvider(IUnitOfWork uow, PaymentsModuleOptions options)
    {
        _uow = uow;
        _options = options;
    }

    public PaymentProviderEnum Provider => PaymentProviderEnum.Virtual;

    public Task<PaymentPrepayResult> CreatePaymentAsync(IPayment payment, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default)
    {
        var prepayId = $"virtual-prepay-{payment.ClientSn}";
        var result = new PaymentPrepayResult
        {
            OutTradeNo = payment.ClientSn,
            PrepayId = prepayId,
            TradeNo = prepayId,
            CodeUrl = channel == PaymentChannelEnum.Precreate ? $"virtual://pay/{payment.ClientSn}" : null,
        };

        if (_options.VirtualTransactionSimulateCallbacks)
            SchedulePaymentCallback(payment.ClientSn, prepayId);
        return Task.FromResult(result);
    }

    public Task<PaymentQueryResult> QueryPaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentQueryResult
        {
            Status = payment.Status,
            TransactionId = payment.TransactionId ?? payment.TradeNo,
            TradeNo = payment.TradeNo,
        });

    public Task<PaymentProviderResult> RevokePaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentProviderResult { Success = true, TradeNo = payment.TradeNo });

    public Task<PaymentProviderResult> CancelPaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentProviderResult { Success = true, TradeNo = payment.TradeNo });

    public Task<PaymentRefundResult> RefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default)
    {
        var result = new PaymentRefundResult
        {
            Success = true,
            RefundTradeNo = $"virtual-refund-{refund.RefundRequestNo}",
            TradeNo = payment.TradeNo,
        };
        if (_options.VirtualTransactionSimulateCallbacks)
            ScheduleRefundCallback(refund.RefundRequestNo, result.RefundTradeNo);
        return Task.FromResult(result);
    }

    public Task<PaymentRefundQueryResult> QueryRefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentRefundQueryResult
        {
            Status = refund.Status,
            RefundTradeNo = refund.RefundTradeNo,
            TradeNo = refund.TradeNo,
        });

    public async Task<PaymentCallbackResult> HandlePaymentNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var payload = System.Text.Json.JsonSerializer.Deserialize<VirtualCallbackPayload>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload?.ClientSn is null)
            return new PaymentCallbackResult { Success = false, ResponseBody = "invalid payload" };

        await _uow.Request(new CompletePaymentRequest(payload.ClientSn, payload.TransactionId ?? $"virtual-tx-{payload.ClientSn}"));
        await _uow.SaveChanges();
        return new PaymentCallbackResult { Success = true, ClientSn = payload.ClientSn };
    }

    public async Task<PaymentCallbackResult> HandleRefundNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var payload = System.Text.Json.JsonSerializer.Deserialize<VirtualRefundCallbackPayload>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload?.RefundRequestNo is null)
            return new PaymentCallbackResult { Success = false, ResponseBody = "invalid payload" };

        await _uow.Request(new CompletePaymentRefundRequest(payload.RefundRequestNo, payload.RefundTradeNo ?? $"virtual-refund-tx-{payload.RefundRequestNo}"));
        await _uow.SaveChanges();
        return new PaymentCallbackResult { Success = true };
    }

    private void SchedulePaymentCallback(string clientSn, string transactionId)
    {
        var delaySeconds = Math.Max(0, _options.VirtualTransactionCallbackDelaySeconds);
        _uow.ScheduleFireAndForgetTask(new VirtualPaymentCallbackTask(new VirtualPaymentCallbackParam(clientSn, transactionId)), afterSaveChange: true, delay: delaySeconds);
    }

    private void ScheduleRefundCallback(string refundRequestNo, string? refundTradeNo)
    {
        var delaySeconds = Math.Max(0, _options.VirtualTransactionCallbackDelaySeconds);
        _uow.ScheduleFireAndForgetTask(new VirtualRefundCallbackTask(new VirtualRefundCallbackParam(refundRequestNo, refundTradeNo)), afterSaveChange: true, delay: delaySeconds);
    }

    private sealed class VirtualCallbackPayload
    {
        public string? ClientSn { get; set; }
        public string? TransactionId { get; set; }
    }

    private sealed class VirtualRefundCallbackPayload
    {
        public string? RefundRequestNo { get; set; }
        public string? RefundTradeNo { get; set; }
    }
}
