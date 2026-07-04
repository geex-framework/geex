using Geex.Extensions.Payment.Requests;

namespace Geex.Extensions.Payment;

public interface IPaymentService
{
    Task<CreatePaymentOrderResult> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, CancellationToken cancellationToken = default);
    Task<IPaymentOrder?> GetPaymentOrderAsync(string outTradeNo, CancellationToken cancellationToken = default);
    Task<IPaymentOrder> ClosePaymentOrderAsync(string outTradeNo, CancellationToken cancellationToken = default);
    Task CompletePaymentAsync(string outTradeNo, string? transactionId, CancellationToken cancellationToken = default);
}
