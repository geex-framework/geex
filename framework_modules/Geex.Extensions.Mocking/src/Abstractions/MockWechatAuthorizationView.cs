namespace Geex.Extensions.Mocking;

public class MockWechatAuthorizationView
{
    public string Token { get; set; } = string.Empty;
    public string? QrSvg { get; set; }
    public MockWechatAuthorizationStatusEnum Status { get; set; } = MockWechatAuthorizationStatusEnum.Pending;
    public string? Code { get; set; }
    public string State { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? ProfileId { get; set; }
}

public class MockWechatProfileSummary
{
    public string Id { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}
