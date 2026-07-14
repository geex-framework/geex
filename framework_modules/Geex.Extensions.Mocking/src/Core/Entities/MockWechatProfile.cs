using System.Text.Json.Nodes;
using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockWechatProfile : Entity<MockWechatProfile>
{
    public MockWechatProfile(string openId, string nickname, string? unionId = null, string? avatar = null, JsonNode? claims = null, bool enabled = true)
    {
        OpenId = openId;
        Nickname = nickname;
        UnionId = unionId;
        Avatar = avatar;
        Claims = claims;
        Enabled = enabled;
    }

    public string OpenId { get; private set; }
    public string? UnionId { get; private set; }
    public string Nickname { get; private set; }
    public string? Avatar { get; private set; }
    public JsonNode? Claims { get; private set; }
    public bool Enabled { get; private set; }

    public void Update(string openId, string nickname, string? unionId, string? avatar, JsonNode? claims, bool enabled)
    {
        OpenId = openId;
        Nickname = nickname;
        UnionId = unionId;
        Avatar = avatar;
        Claims = claims;
        Enabled = enabled;
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public Dictionary<string, string> ToUserInfoDictionary()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["openid"] = OpenId,
            ["nickname"] = Nickname,
        };
        if (!string.IsNullOrWhiteSpace(UnionId))
        {
            result["unionid"] = UnionId;
        }
        if (!string.IsNullOrWhiteSpace(Avatar))
        {
            result["avatar"] = Avatar!;
        }

        if (Claims is JsonObject claimObject)
        {
            foreach (var (key, value) in claimObject)
            {
                if (value is null)
                {
                    continue;
                }

                result[key] = value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue)
                    ? stringValue
                    : value.ToJsonString();
            }
        }

        return result;
    }

    public override Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(OpenId))
        {
            return Task.FromResult(new ValidationResult("OpenId is required.", new[] { nameof(OpenId) }));
        }

        if (string.IsNullOrWhiteSpace(Nickname))
        {
            return Task.FromResult(new ValidationResult("Nickname is required.", new[] { nameof(Nickname) }));
        }

        return Task.FromResult(ValidationResult.Success);
    }
}
