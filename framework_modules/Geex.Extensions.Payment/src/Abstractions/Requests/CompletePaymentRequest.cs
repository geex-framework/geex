using MediatX;

namespace Geex.Extensions.Payment.Requests;

public record CompletePaymentRequest(string OutTradeNo, string? TransactionId) : IRequest<IPaymentOrder>;
