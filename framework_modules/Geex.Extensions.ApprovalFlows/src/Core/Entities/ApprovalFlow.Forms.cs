using System;
using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlow
{
    public List<string> Categories { get; set; } = [];
    public List<ApprovalFlowFormField> FormFields { get; set; } = [];
    public List<ApprovalFormValue> FormValues { get; set; } = [];
    public string? TemplateName { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public string? ReplacedByFlowId { get; set; }

    public bool CanEditForm => Status == ApprovalFlowStatus.Processing && !string.IsNullOrWhiteSpace(CreatorUserId)
        && CreatorUserId == ServiceProvider.GetRequiredService<ICurrentUser>().UserId;

    private ApprovalFlowTemplate? InitializeSnapshot(IApprovalFlowDate data)
    {
        if (string.IsNullOrWhiteSpace(data.TemplateId))
        {
            FormValues = ApprovalFormValidation.Values(FormFields, data.FormValues, true);
            return null;
        }
        var template = Uow.Query<ApprovalFlowTemplate>().GetById(data.TemplateId)
            ?? throw new UserFriendlyException("审批流模板不存在.");
        if (template.Nodes.Count != data.Nodes.Count)
            throw new UserFriendlyException("审批节点与模板不一致, 请重新选择模板.");
        Categories = [.. template.Categories];
        FormFields = template.FormFields.Select(x => x.Copy()).ToList();
        FormValues = ApprovalFormValidation.Values(FormFields, data.FormValues, true);
        TemplateName = template.Name;
        return template;
    }

    public void SaveForm(List<ApprovalFormValue> values)
    {
        if (!CanEditForm) throw new UserFriendlyException("仅流程发起人可在办理期间修改流程表单.");
        var validated = ApprovalFormValidation.Values(FormFields, values, true);
        foreach (var field in FormFields.Where(x => !x.AllowEdit))
        {
            var previous = FormValues.FirstOrDefault(x => x.Key == field.Key)?.Values ?? [];
            var proposed = validated.FirstOrDefault(x => x.Key == field.Key)?.Values ?? [];
            if (!previous.SequenceEqual(proposed))
                throw new UserFriendlyException($"{field.Name} 在创建后只读.");
        }
        FormValues = validated;
        var node = ActiveNode ?? Nodes.First();
        Uow.Create(node.Id, ApprovalFlowNodeLogType.Edit, CreatorUserId, "", "更新流程表单");
    }

    public void Archive(string replacementId)
    {
        if (Status != ApprovalFlowStatus.Canceled)
            throw new UserFriendlyException("仅已取消的流程可在重新上报时归档.");
        Status = ApprovalFlowStatus.Archived;
        ArchivedAt = DateTimeOffset.UtcNow;
        ReplacedByFlowId = replacementId;
    }
}
