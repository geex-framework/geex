using MediatX;

namespace Geex.Extensions.Payment.Requests;

public record ClosePaymentOrderRequest(string OutTradeNo) : IRequest<IPaymentOrder>;
