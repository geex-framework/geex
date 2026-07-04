using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Payments;

public interface IPaymentProvider
{
    PaymentProviderEnum Provider { get; }
    Task<PaymentPrepayResult> CreatePaymentAsync(IPayment payment, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default);
    Task<PaymentQueryResult> QueryPaymentAsync(IPayment payment, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> RevokePaymentAsync(IPayment payment, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> CancelPaymentAsync(IPayment payment, CancellationToken cancellationToken = default);
    Task<PaymentRefundResult> RefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default);
    Task<PaymentRefundQueryResult> QueryRefundAsync(IPayment payment, IPaymentRefund refund, CancellationToken cancellationToken = default);
    Task<PaymentCallbackResult> HandlePaymentNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default);
    Task<PaymentCallbackResult> HandleRefundNotifyAsync(HttpRequest request, CancellationToken cancellationToken = default);
}
