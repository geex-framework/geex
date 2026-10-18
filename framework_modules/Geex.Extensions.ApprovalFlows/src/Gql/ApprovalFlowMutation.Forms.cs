using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.Settings;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows.Gql;

public partial class ApprovalFlowMutation
{
    public ApprovalFlow SaveApprovalFlowForm(string id, List<ApprovalFormValue> values)
    {
        var flow = _uow.Query<ApprovalFlow>().GetById(id);
        flow.SaveForm(values);
        return flow;
    }

    public ApprovalFlowNode SaveApprovalFlowNodeForm(string nodeId, List<ApprovalFormValue> values)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        node.SaveForm(values);
        return node;
    }

    public async Task<ApprovalFlowNode> ConsultApprovalFlowNodeUsers(string nodeId, List<string> userIds, string message)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.ConsultUsers(userIds, message);
        return node;
    }

    public async Task<List<string>> SaveApprovalFlowTags(List<string> tags)
    {
        var normalized = tags.Select(x => x.Trim()).ToList();
        if (normalized.Any(string.IsNullOrWhiteSpace) || normalized.Distinct().Count() != normalized.Count)
            throw new UserFriendlyException("流程分类不能为空或重复.");
        var tenant = _uow.ServiceProvider.GetService<ICurrentTenant>()?.Code;
        var scopeKey = string.IsNullOrEmpty(tenant) ? null : tenant;
        var settings = _uow.ServiceProvider.GetRequiredService<ISettingService>();
        await settings.SetAsync(ApprovalFlowSettings.Tags,
            scopeKey == null ? SettingScopeEnumeration.Global : SettingScopeEnumeration.Tenant,
            scopeKey, JsonSerializer.SerializeToNode(normalized));
        return normalized;
    }
}
