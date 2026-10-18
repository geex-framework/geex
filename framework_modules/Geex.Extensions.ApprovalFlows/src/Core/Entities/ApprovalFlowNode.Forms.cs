using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;
using Volo.Abp;
using Geex.Gql.Attributes;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public class ApprovalFlowConsultation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = "";
    public string FromUserId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reply { get; set; }
    public DateTimeOffset? RepliedAt { get; set; }
}

public partial class ApprovalFlowNode : ISaveIntercepted
{
    [HotChocolate.GraphQLIgnore]
    public string Revision { get; set; } = "";

    private void Touch()
    {
        Revision = Guid.NewGuid().ToString("N");
        ApprovalFlow.Value.Touch();
    }

    async Task ISaveIntercepted.InterceptOnSave(IEntityBase originalValue)
    {
        if (originalValue is ApprovalFlowNode previous)
            await ApprovalFlowConcurrency.Guard<ApprovalFlowNode>(Uow, Id, previous.Revision, Revision, x => x.Revision);
    }
    public List<ApprovalFormField> FormFields { get; set; } = [];
    public List<ApprovalFormValue> FormValues { get; set; } = [];
    public List<ApprovalFlowConsultation> Consultations { get; set; } = [];
    [AutoBatchLoadDependsOn(nameof(ApprovalFlow))]
    public bool IsActive => ApprovalFlow.Value.Status == ApprovalFlowStatus.Processing
        && ApprovalFlow.Value.ActiveIndex == Index
        && (NodeStatus & (ApprovalFlowNodeStatus.Approved | ApprovalFlowNodeStatus.Rejected | ApprovalFlowNodeStatus.Transferred)) == 0;
    [AutoBatchLoadDependsOn(nameof(ApprovalFlow))]
    public bool CanOperate => IsActive && !string.IsNullOrWhiteSpace(AuditUserId)
        && AuditUserId == CurrentUser.UserId;
    [AutoBatchLoadDependsOn(nameof(ApprovalFlow))]
    public bool CanReply => IsActive && NodeStatus.HasFlag(ApprovalFlowNodeStatus.Consulting)
        && (Consultations.Any(x => x.UserId == CurrentUser.UserId && x.RepliedAt == null)
            || (Consultations.Count == 0 && !string.IsNullOrEmpty(ConsultUserId) && ConsultUserId == CurrentUser.UserId));
    [AutoBatchLoadDependsOn(nameof(ApprovalFlow))]
    public bool CanEditForm => ApprovalFlow.Value.Status == ApprovalFlowStatus.Processing
        && !string.IsNullOrWhiteSpace(AuditUserId) && AuditUserId == CurrentUser.UserId
        && !NodeStatus.HasFlag(ApprovalFlowNodeStatus.Approved);

    public void SaveForm(List<ApprovalFormValue> values)
    {
        if (!CanEditForm) throw new UserFriendlyException("仅节点审批人可在节点通过前修改节点表单.");
        var validated = ApprovalFormValidation.Values(FormFields, values, false);
        ApprovalFormValidation.References(Uow, FormFields, validated);
        FormValues = validated;
        Touch();
        Uow.Create(Id, ApprovalFlowNodeLogType.Edit, CurrentUser.UserId, "", "更新节点表单");
    }

    private void CheckActive()
    {
        if (!IsActive)
            throw new UserFriendlyException("仅当前办理节点可执行此操作.");
    }
}
