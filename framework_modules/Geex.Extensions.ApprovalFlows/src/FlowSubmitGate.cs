using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Geex.Extensions.Settings;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;

namespace Geex.Extensions.ApprovalFlows;

public static class FlowSubmitGate
{
    public static async Task<bool> IsEnabled(IUnitOfWork uow, string moduleKey)
    {
        var config = await GetConfiguration(uow);
        return config is JsonObject modules
            && modules[moduleKey] is JsonObject module
            && module["useFlow"] is JsonValue value
            && value.TryGetValue<bool>(out var enabled)
            && enabled;
    }

    internal static async Task<JsonNode> GetConfiguration(IUnitOfWork uow)
    {
        var setting = await uow.ServiceProvider.GetRequiredService<ISettingService>()
            .GetSetting(ApprovalFlowSettings.FlowSubmitConfig);
        return setting?.Value ?? new JsonObject();
    }

    public static async Task DefaultSubmit<TInterface, TEntity>(IUnitOfWork uow, string[] ids, string? remark)
        where TInterface : IApproveEntity, IEntityBase where TEntity : TInterface
    {
        var entities = uow.Query<TEntity>().Where(x => ids.Contains(x.Id)).ToList();
        if (!entities.Any())
            throw new BusinessException(GeexExceptionType.NotFound);

        foreach (var entity in entities)
        {
            if (entity is { DbContext: null })
                uow.Attach(entity);
            await entity.Submit<TInterface>(remark);
        }
    }
}
