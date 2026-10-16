using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

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

public partial class ApprovalFlowNode
{
    public List<ApprovalFormField> FormFields { get; set; } = [];
    public List<ApprovalFormValue> FormValues { get; set; } = [];
    public List<ApprovalFlowConsultation> Consultations { get; set; } = [];
    public bool CanEditForm => ApprovalFlow.Value.Status == ApprovalFlowStatus.Processing
        && !string.IsNullOrWhiteSpace(AuditUserId) && AuditUserId == CurrentUser.UserId
        && !NodeStatus.HasFlag(ApprovalFlowNodeStatus.Approved);

    public void SaveForm(List<ApprovalFormValue> values)
    {
        if (!CanEditForm) throw new UserFriendlyException("仅节点审批人可在节点通过前修改节点表单.");
        FormValues = ApprovalFormValidation.Values(FormFields, values, false);
        Uow.Create(Id, ApprovalFlowNodeLogType.Edit, CurrentUser.UserId, "", "更新节点表单");
    }

    private void CheckActive()
    {
        if (ApprovalFlow.Value.Status != ApprovalFlowStatus.Processing || ApprovalFlow.Value.ActiveIndex != Index
            || (NodeStatus & (ApprovalFlowNodeStatus.Approved | ApprovalFlowNodeStatus.Rejected | ApprovalFlowNodeStatus.Transferred)) != 0)
            throw new UserFriendlyException("仅当前办理节点可执行此操作.");
    }
}
