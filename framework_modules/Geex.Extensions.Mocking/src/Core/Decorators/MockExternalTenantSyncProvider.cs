using System.Text.Json;
using System.Text.Json.Nodes;
using Geex.MultiTenant;
using Geex.Storage;

namespace Geex.Extensions.Mocking.Core.Decorators;

public class MockExternalTenantSyncProvider : IExternalTenantSyncProvider
{
    private readonly IExternalTenantSyncProvider _inner;
    private readonly IUnitOfWork _uow;

    public MockExternalTenantSyncProvider(IExternalTenantSyncProvider inner, IUnitOfWork uow)
    {
        _inner = inner;
        _uow = uow;
    }

    public async Task<ITenant> SyncAsync(string code, ITenant localTenant, CancellationToken cancellationToken = default)
    {
        var input = JsonSerializer.SerializeToNode(new { code, localTenant.Name, localTenant.IsEnabled });
        var rule = _uow.FindMatchingRule(MockTargetEnum.ExternalTenantSyncProvider, nameof(SyncAsync), input);
        if (rule is null)
        {
            return await _inner.SyncAsync(code, localTenant, cancellationToken);
        }

        await rule.ApplyDelayAsync(cancellationToken);
        if (rule.Outcome == MockOutcomeEnum.Throw)
        {
            throw rule.CreateThrowException();
        }

        if (rule.Response is JsonObject response)
        {
            if (response.TryGetPropertyValue("code", out var codeNode) && codeNode is not null)
            {
                localTenant.Code = codeNode.GetValue<string>();
            }

            if (response.TryGetPropertyValue("name", out var nameNode) && nameNode is not null)
            {
                localTenant.Name = nameNode.GetValue<string>();
            }

            if (response.TryGetPropertyValue("isEnabled", out var enabledNode) && enabledNode is not null)
            {
                localTenant.IsEnabled = enabledNode.GetValue<bool>();
            }

            if (response.TryGetPropertyValue("externalInfo", out var externalInfo))
            {
                localTenant.ExternalInfo = externalInfo?.DeepClone();
            }
        }

        return localTenant;
    }
}
