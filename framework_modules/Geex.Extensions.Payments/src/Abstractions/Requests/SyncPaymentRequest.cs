using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record SyncPaymentRequest(string ClientSn) : IRequest<IPayment>;
