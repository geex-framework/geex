using System.Text.Json.Nodes;
using MediatX;

namespace Geex.Extensions.Payment.Requests;

public record CreatePaymentOrderRequest : IRequest<CreatePaymentOrderResult>
{
    public decimal Amount { get; set; }
    public string Subject { get; set; } = string.Empty;
    public PaymentProviderEnum Provider { get; set; } = PaymentProviderEnum.Mock;
    public PaymentChannelEnum Channel { get; set; } = PaymentChannelEnum.Native;
    public string? BusinessOrderId { get; set; }
    public string? Currency { get; set; }
    public string? OpenId { get; set; }
    public string? BuyerId { get; set; }
    public JsonNode? ExtraData { get; set; }
}
