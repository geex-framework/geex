using MediatX;

namespace Geex.Extensions.Payment.Requests;

public record GetPaymentOrderRequest(string OutTradeNo) : IRequest<IPaymentOrder?>;
