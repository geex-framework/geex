using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geex.Extensions.Payments.Core.Providers.Shouqianba;

public class ShouqianbaApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly PaymentsModuleOptions _options;

    public ShouqianbaApiClient(HttpClient httpClient, PaymentsModuleOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri(_options.ApiDomain.TrimEnd('/') + "/");
    }

    public async Task<ShouqianbaBizResponse<TData>> PostAsync<TData>(string path, object body, ShouqianbaCredentials credentials, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var sign = ShouqianbaSignature.ComputeSign(json, credentials.TerminalKey);
        using var request = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue(credentials.TerminalSn, sign);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<ShouqianbaBizResponse<TData>>(responseBody, JsonOptions)
            ?? new ShouqianbaBizResponse<TData> { ResultCode = "FAIL", ErrorMessage = "Empty response." };
        result.RawBody = responseBody;
        return result;
    }
}

public class ShouqianbaBizResponse<TData>
{
    public string ResultCode { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public TData? BizResponse { get; set; }
    [JsonIgnore] public string? RawBody { get; set; }
}

public class ShouqianbaPaymentData
{
    public string? Sn { get; set; }
    public string? ClientSn { get; set; }
    public string? TradeNo { get; set; }
    public string? Status { get; set; }
    public string? OrderStatus { get; set; }
    public string? QrCode { get; set; }
    public string? TotalAmount { get; set; }
    public string? NetAmount { get; set; }
}

public class ShouqianbaRefundData
{
    public string? Sn { get; set; }
    public string? ClientSn { get; set; }
    public string? TradeNo { get; set; }
    public string? RefundAmount { get; set; }
    public string? NetAmount { get; set; }
}

public class ShouqianbaNotifyPayload
{
    public string? Sn { get; set; }
    public string? ClientSn { get; set; }
    public string? TradeNo { get; set; }
    public string? Status { get; set; }
    public string? OrderStatus { get; set; }
    public string? RefundRequestNo { get; set; }
    public string? RefundAmount { get; set; }
}
