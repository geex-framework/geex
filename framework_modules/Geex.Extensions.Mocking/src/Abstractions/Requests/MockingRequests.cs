using System.Text.Json.Nodes;
using MediatX;

namespace Geex.Extensions.Mocking.Requests;

public record GetMockRulesRequest : IRequest<IQueryable<Core.Entities.MockRule>>;
public record CreateMockRuleRequest : IRequest<Core.Entities.MockRule>
{
    public string Name { get; set; } = string.Empty;
    public MockTargetEnum Target { get; set; } = MockTargetEnum.WechatApiClient;
    public string Operation { get; set; } = string.Empty;
    public MockOutcomeEnum Outcome { get; set; } = MockOutcomeEnum.Return;
    public JsonNode? Match { get; set; }
    public JsonNode? Response { get; set; }
    public int Priority { get; set; }
    public int DelayMilliseconds { get; set; }
    public bool Enabled { get; set; } = true;
}
public record EditMockRuleRequest : IRequest<Core.Entities.MockRule>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MockTargetEnum Target { get; set; } = MockTargetEnum.WechatApiClient;
    public string Operation { get; set; } = string.Empty;
    public MockOutcomeEnum Outcome { get; set; } = MockOutcomeEnum.Return;
    public JsonNode? Match { get; set; }
    public JsonNode? Response { get; set; }
    public int Priority { get; set; }
    public int DelayMilliseconds { get; set; }
    public bool Enabled { get; set; } = true;
}
public record DeleteMockRuleRequest : IRequest<bool>
{
    public string Id { get; set; } = string.Empty;
}
public record SetMockRuleEnabledRequest : IRequest<Core.Entities.MockRule>
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public record GetMockWechatProfilesRequest : IRequest<IQueryable<Core.Entities.MockWechatProfile>>;
public record CreateMockWechatProfileRequest : IRequest<Core.Entities.MockWechatProfile>
{
    public string OpenId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? Avatar { get; set; }
    public JsonNode? Claims { get; set; }
    public bool Enabled { get; set; } = true;
}
public record EditMockWechatProfileRequest : IRequest<Core.Entities.MockWechatProfile>
{
    public string Id { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? Avatar { get; set; }
    public JsonNode? Claims { get; set; }
    public bool Enabled { get; set; } = true;
}
public record DeleteMockWechatProfileRequest : IRequest<bool>
{
    public string Id { get; set; } = string.Empty;
}

public record CreateMockWechatAuthorizationRequest : IRequest<MockWechatAuthorizationView>
{
    public string RedirectUri { get; set; } = string.Empty;
    public string State { get; set; } = "WechatWeb";
}
public record ConfirmMockWechatAuthorizationRequest : IRequest<MockWechatAuthorizationView>
{
    public string Token { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
}
public record GetMockWechatAuthorizationStatusRequest : IRequest<MockWechatAuthorizationView>
{
    public string Token { get; set; } = string.Empty;
}
public record GetMockWechatAuthorizeProfilesRequest : IRequest<List<MockWechatProfileSummary>>
{
    public string Token { get; set; } = string.Empty;
}

public record GetMockPaymentTransactionByTokenRequest : IRequest<Core.Entities.MockPaymentTransaction>
{
    public string Token { get; set; } = string.Empty;
}
public record ConfirmMockPaymentTransactionRequest : IRequest<Core.Entities.MockPaymentTransaction>
{
    public string Token { get; set; } = string.Empty;
    public MockPaymentTransactionStatusEnum Status { get; set; } = MockPaymentTransactionStatusEnum.Succeeded;
}

public record GetMockSmsMessagesRequest : IRequest<IQueryable<Core.Entities.MockSmsMessage>>
{
    public string? PhoneNumber { get; set; }
}
public record ClearMockSmsMessagesRequest : IRequest<bool>
{
    public string? PhoneNumber { get; set; }
}
