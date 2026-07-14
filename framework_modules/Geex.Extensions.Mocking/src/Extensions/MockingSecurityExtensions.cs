using System.Text.Json.Nodes;
using Geex.Extensions.Authentication;
using Geex.Extensions.Mocking.Core.Entities;
using Geex.Storage;

namespace Geex.Extensions.Mocking;

public static class MockingSecurityExtensions
{
    public static void EnsureSuperAdmin(this ICurrentUser currentUser)
    {
        if (!currentUser.IsSuperAdmin)
        {
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "AUTH_NOT_AUTHORIZED");
        }
    }

    public static MockRule? FindMatchingRule(this IUnitOfWork uow, MockTargetEnum target, string operation, JsonNode? input)
    {
        var candidates = uow.Query<MockRule>()
            .Where(x => x.Enabled && x.Target == target && x.Operation == operation)
            .ToList();
        return MockRule.SelectBest(candidates, target, operation, input);
    }
}
