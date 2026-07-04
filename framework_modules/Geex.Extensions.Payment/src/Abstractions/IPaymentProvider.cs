using Microsoft.AspNetCore.Http;

namespace Geex.Extensions.Payment;

public interface IPaymentProvider
{
    PaymentProviderEnum Provider { get; }
    Task<PaymentPrepayResult> CreatePaymentAsync(IPaymentOrder order, PaymentChannelEnum channel, PaymentCreateContext context, CancellationToken cancellationToken = default);
    Task<PaymentQueryResult> QueryPaymentAsync(IPaymentOrder order, CancellationToken cancellationToken = default);
    Task<PaymentCallbackResult> HandleCallbackAsync(HttpRequest request, CancellationToken cancellationToken = default);
}
