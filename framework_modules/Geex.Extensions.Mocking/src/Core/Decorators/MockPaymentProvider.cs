using System.Text.Json;
using System.Text.Json.Nodes;
using Geex.Extensions.Mocking.Core.Entities;
using Geex.Extensions.Payments;
using Geex.MultiTenant;
using Geex.Storage;
using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Mocking.Core.Decorators;

public class MockPaymentProvider : IPaymentProvider
{
    private readonly IPaymentProvider _inner;
    private readonly IUnitOfWork _uow;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LazyService<ICurrentTenant>? _currentTenant;

    public MockPaymentProvider(IPaymentProvider inner, IUnitOfWork uow, IHttpContextAccessor httpContextAccessor, LazyService<ICurrentTenant>? currentTenant = null)
    {
        _inner = inner;
        _uow = uow;
        _httpContextAccessor = httpContextAccessor;
        _currentTenant = currentTenant;
    }

    public PaymentProviderEnum Provider => _inner.Provider;

    public async Task<PaymentPrepayResult> CreatePaymentAsync(IPayment payment, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default)
    {
        var input = JsonSerializer.SerializeToNode(new
        {
            payment.ClientSn,
            channel = channel.Value,
            payment.Amount,
            payment.Subject,
        });
        var rule = _uow.FindMatchingRule(MockTargetEnum.PaymentProvider, nameof(CreatePaymentAsync), input);
        if (rule is not null)
        {
            await rule.ApplyDelayAsync(cancellationToken);
            if (rule.Outcome == MockOutcomeEnum.Throw)
            {
                throw rule.CreateThrowException();
            }

            return rule.DeserializeResponse<PaymentPrepayResult>();
        }

        var origin = ResolveTrustedOrigin();
        var token = MockRule.CreateOpaqueToken();
        var transaction = _uow.Attach(new MockPaymentTransaction(
            token,
            payment.ClientSn,
            origin,
            payment.Amount,
            payment.Currency,
            payment.Subject,
            payment.Provider,
            channel,
            DateTimeOffset.Now.AddMinutes(30),
            _currentTenant?.Value?.Code));
        await _uow.SaveChanges(cancellationToken);

        return new PaymentPrepayResult
        {
            OutTradeNo = payment.ClientSn,
            PrepayId = transaction.PrepayId,
            TradeNo = transaction.TradeNo,
            CodeUrl = channel == PaymentChannelEnum.Precreate ? $"{origin}/mocking/payments/{token}" : null,
        };
    }

    public async Task<PaymentQueryResult> QueryPaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
    {
        var transaction = _uow.Query<MockPaymentTransaction>().FirstOrDefault(x => x.ClientSn == payment.ClientSn);
        if (transaction is not null)
        {
            return new PaymentQueryResult
            {
                Status = transaction.ToPaymentStatus(),
                TransactionId = transaction.TransactionId,
                TradeNo = transaction.TradeNo,
            };
        }

        return await _inner.QueryPaymentAsync(payment, cancellationToken);
    }

    public async Task<PaymentProviderResult> RevokePaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
    {
        var transaction = _uow.Query<MockPaymentTransaction>().FirstOrDefault(x => x.ClientSn == payment.ClientSn);
        if (transaction is not null)
        {
            transaction.MarkRevoked();
            await _uow.SaveChanges(cancellationToken);
            return new PaymentProviderResult { Success = true, TradeNo = transaction.TradeNo };
        }

        return await _inner.RevokePaymentAsync(payment, cancellationToken);
    }

    public async Task<PaymentProviderResult> CancelPaymentAsync(IPayment payment, CancellationToken cancellationToken = default)
    {
        var transaction = _uow.Query<MockPaymentTransaction>().FirstOrDefault(x => x.ClientSn == payment.ClientSn);
        if (transaction is not null)
        {
            transaction.ConfirmClosed();
            await _uow.SaveChanges(cancellationToken);
            return new PaymentProviderResult { Success = true, TradeNo = transaction.TradeNo };
        }

        return await _inner.CancelPaymentAsync(payment, cancellationToken);
    }

    public async Task<PaymentRefundResult> RefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default)
    {
        var input = JsonSerializer.SerializeToNode(new { payment.ClientSn, refund.RefundRequestNo, refund.Amount });
        var rule = _uow.FindMatchingRule(MockTargetEnum.PaymentProvider, nameof(RefundAsync), input);
        if (rule is not null)
        {
            await rule.ApplyDelayAsync(cancellationToken);
            if (rule.Outcome == MockOutcomeEnum.Throw)
            {
                throw rule.CreateThrowException();
            }

            return rule.DeserializeResponse<PaymentRefundResult>();
        }

        var mockRefund = _uow.Attach(new MockPaymentRefund(payment.ClientSn, refund.RefundRequestNo, refund.Amount, payment.TradeNo));
        mockRefund.MarkSucceeded();
        await _uow.SaveChanges(cancellationToken);
        return new PaymentRefundResult
        {
            Success = true,
            RefundTradeNo = mockRefund.RefundTradeNo,
            TradeNo = payment.TradeNo,
        };
    }

    public async Task<PaymentRefundQueryResult> QueryRefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default)
    {
        var mockRefund = _uow.Query<MockPaymentRefund>().FirstOrDefault(x => x.RefundRequestNo == refund.RefundRequestNo);
        if (mockRefund is not null)
        {
            return new PaymentRefundQueryResult
            {
                Status = mockRefund.Status,
                RefundTradeNo = mockRefund.RefundTradeNo,
                TradeNo = mockRefund.TradeNo,
            };
        }

        return await _inner.QueryRefundAsync(payment, refund, cancellationToken);
    }

    public Task<PaymentCallbackResult> HandlePaymentNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default)
        => _inner.HandlePaymentNotifyAsync(request, cancellationToken);

    public Task<PaymentCallbackResult> HandleRefundNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default)
        => _inner.HandleRefundNotifyAsync(request, cancellationToken);

    private string ResolveTrustedOrigin()
    {
        var request = _httpContextAccessor.HttpContext?.Request
                      ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "HttpContext is required to build mock payment URL.");
        var origin = request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return origin.TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host}".TrimEnd('/');
    }
}
