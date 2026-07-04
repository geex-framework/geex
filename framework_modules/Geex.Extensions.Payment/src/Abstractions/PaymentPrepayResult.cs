using System.Text.Json.Nodes;

namespace Geex.Extensions.Payment;

public class PaymentPrepayResult
{
    public string OutTradeNo { get; set; } = string.Empty;
    public string? CodeUrl { get; set; }
    public JsonNode? JsApiParams { get; set; }
    public string? PagePayForm { get; set; }
    public string? PrepayId { get; set; }
}
