using System.Text.Json;
using Geex.Extensions.Payments.Infrastructure;
using Geex.Extensions.Payments.Requests;
using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Payments.Core.Providers.Shouqianba;

public class ShouqianbaPaymentProvider : IPaymentProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly ShouqianbaApiClient _apiClient;
    private readonly ShouqianbaCredentialsProvider _credentialsProvider;
    private readonly IUnitOfWork _uow;

    public ShouqianbaPaymentProvider(ShouqianbaApiClient apiClient, ShouqianbaCredentialsProvider credentialsProvider, IUnitOfWork uow)
    {
        _apiClient = apiClient;
        _credentialsProvider = credentialsProvider;
        _uow = uow;
    }

    public PaymentProviderEnum Provider => PaymentProviderEnum.Shouqianba;

    public async Task<PaymentPrepayResult> CreatePaymentAsync(IPayment payment, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["terminal_sn"] = credentials.TerminalSn,
            ["client_sn"] = payment.ClientSn,
            ["total_amount"] = ShouqianbaAmount.ToFen(payment.Amount),
            ["subject"] = payment.Subject,
            ["notify_url"] = context.NotifyUrl,
        };

        var path = channel == PaymentChannelEnum.Pay ? "upay/v2/pay" : "upay/v2/precreate";
        if (channel == PaymentChannelEnum.Pay)
            body["dynamic_id"] = context.AuthCode;

        var response = await _apiClient.PostAsync<ShouqianbaPaymentData>(path, body, credentials, cancellationToken);
        var data = response.BizResponse;
        if (!IsSuccess(response.ResultCode) || data is null)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: response.ErrorMessage ?? "Shouqianba create payment failed.");

        return new PaymentPrepayResult
        {
            OutTradeNo = payment.ClientSn,
            PrepayId = data.Sn,
            CodeUrl = data.QrCode,
            TradeNo = data.TradeNo ?? data.Sn,
        };
    }

    public async Task<PaymentQueryResult> QueryPaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["terminal_sn"] = credentials.TerminalSn,
            ["client_sn"] = payment.ClientSn,
        };
        var response = await _apiClient.PostAsync<ShouqianbaPaymentData>("upay/v2/query", body, credentials, cancellationToken);
        var data = response.BizResponse;
        if (!IsSuccess(response.ResultCode) || data is null)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: response.ErrorMessage ?? "Shouqianba query payment failed.");

        return new PaymentQueryResult
        {
            Status = ShouqianbaOrderStatusMapper.MapPaymentStatus(data.OrderStatus ?? data.Status),
            TransactionId = data.TradeNo ?? data.Sn,
            TradeNo = data.TradeNo ?? data.Sn,
        };
    }

    public async Task<PaymentProviderResult> RevokePaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["terminal_sn"] = credentials.TerminalSn,
            ["client_sn"] = payment.ClientSn,
        };
        var response = await _apiClient.PostAsync<ShouqianbaPaymentData>("upay/v2/revoke", body, credentials, cancellationToken);
        return MapProviderResult(response);
    }

    public async Task<PaymentProviderResult> CancelPaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["terminal_sn"] = credentials.TerminalSn,
            ["client_sn"] = payment.ClientSn,
        };
        var response = await _apiClient.PostAsync<ShouqianbaPaymentData>("upay/v2/cancel", body, credentials, cancellationToken);
        return MapProviderResult(response);
    }

    public async Task<PaymentRefundResult> RefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["terminal_sn"] = credentials.TerminalSn,
            ["client_sn"] = payment.ClientSn,
            ["refund_request_no"] = refund.RefundRequestNo,
            ["refund_amount"] = ShouqianbaAmount.ToFen(refund.Amount),
            ["operator"] = "system",
        };
        var response = await _apiClient.PostAsync<ShouqianbaRefundData>("upay/v2/refund", body, credentials, cancellationToken);
        var data = response.BizResponse;
        return new PaymentRefundResult
        {
            Success = IsSuccess(response.ResultCode),
            RefundTradeNo = data?.Sn,
            TradeNo = data?.TradeNo,
            Message = response.ErrorMessage,
        };
    }

    public async Task<PaymentRefundQueryResult> QueryRefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["terminal_sn"] = credentials.TerminalSn,
            ["client_sn"] = payment.ClientSn,
            ["refund_request_no"] = refund.RefundRequestNo,
        };
        var response = await _apiClient.PostAsync<ShouqianbaRefundData>("upay/v2/query", body, credentials, cancellationToken);
        return new PaymentRefundQueryResult
        {
            Status = ShouqianbaOrderStatusMapper.MapRefundStatus(response.ResultCode),
            RefundTradeNo = response.BizResponse?.Sn,
            TradeNo = response.BizResponse?.TradeNo,
        };
    }

    public async Task<PaymentCallbackResult> HandlePaymentNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        if (!ShouqianbaSignature.TryParseAuthorization(request.Headers.Authorization, out var terminalSn, out var sign))
            return FailCallback("invalid authorization");

        var credentials = await _credentialsProvider.TryGetCredentialsByTerminalSnAsync(terminalSn, cancellationToken);
        if (credentials is null || !ShouqianbaSignature.TryVerify(body, terminalSn, sign, credentials.TerminalKey, out _))
            return FailCallback("invalid signature");

        var payload = JsonSerializer.Deserialize<ShouqianbaNotifyPayload>(body, JsonOptions);
        if (payload?.ClientSn is null)
            return FailCallback("invalid payload");

        var status = ShouqianbaOrderStatusMapper.MapPaymentStatus(payload.OrderStatus ?? payload.Status);
        if (status == PaymentStatusEnum.Succeeded)
        {
            await _uow.Request(new CompletePaymentRequest(payload.ClientSn, payload.TradeNo ?? payload.Sn));
            await _uow.SaveChanges();
        }

        return new PaymentCallbackResult { Success = true, ClientSn = payload.ClientSn, TransactionId = payload.TradeNo };
    }

    public async Task<PaymentCallbackResult> HandleRefundNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        if (!ShouqianbaSignature.TryParseAuthorization(request.Headers.Authorization, out var terminalSn, out var sign))
            return FailCallback("invalid authorization");

        var credentials = await _credentialsProvider.TryGetCredentialsByTerminalSnAsync(terminalSn, cancellationToken);
        if (credentials is null || !ShouqianbaSignature.TryVerify(body, terminalSn, sign, credentials.TerminalKey, out _))
            return FailCallback("invalid signature");

        var payload = JsonSerializer.Deserialize<ShouqianbaNotifyPayload>(body, JsonOptions);
        if (payload?.RefundRequestNo is null)
            return FailCallback("invalid payload");

        await _uow.Request(new CompletePaymentRefundRequest(payload.RefundRequestNo, payload.Sn ?? payload.TradeNo));
        await _uow.SaveChanges();

        return new PaymentCallbackResult { Success = true, ClientSn = payload.ClientSn };
    }

    private static bool IsSuccess(string? resultCode)
        => string.Equals(resultCode, "200", StringComparison.OrdinalIgnoreCase)
           || string.Equals(resultCode, "SUCCESS", StringComparison.OrdinalIgnoreCase);

    private static PaymentProviderResult MapProviderResult(ShouqianbaBizResponse<ShouqianbaPaymentData> response)
        => new()
        {
            Success = IsSuccess(response.ResultCode),
            TradeNo = response.BizResponse?.TradeNo ?? response.BizResponse?.Sn,
            Message = response.ErrorMessage,
        };

    private static async Task<string> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;
        return body;
    }

    private static PaymentCallbackResult FailCallback(string message)
        => new() { Success = false, ResponseBody = message, ContentType = "text/plain" };
}
