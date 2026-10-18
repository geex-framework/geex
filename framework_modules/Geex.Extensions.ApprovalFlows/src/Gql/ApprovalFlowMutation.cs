using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Geex.Extensions.Authentication;
using Geex.Extensions.Settings;
using Geex.Extensions.Identity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Geex.Abstractions;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.ApprovalFlows.Gql;

public partial class ApprovalFlowMutation : MutationExtension<ApprovalFlowMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ApprovalFlowMutation> descriptor)
    {
        foreach (var method in typeof(ApprovalFlowMutation).GetMethods(System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            descriptor.Field(method).Authorize();
        descriptor.Field(x => x.SaveApprovalFlowCategories(default)).Authorize(SettingsPermission.Edit);
        base.Configure(descriptor);
    }

    private IUnitOfWork _uow;

    public ApprovalFlowMutation(IUnitOfWork uow) => _uow = uow;

    public async Task<ApprovalFlow> CreateApprovalFlow(CreateApprovalFlowRequest request)
    {
        if ((request.AssociatedEntityType == null) != string.IsNullOrWhiteSpace(request.AssociatedEntityId))
            throw new UserFriendlyException("请同时提供关联对象类型和编号.");
        if (request.AssociatedEntityType != null && !typeof(IApproveEntity).IsAssignableFrom(request.AssociatedEntityType.Type))
            throw new UserFriendlyException("关联对象类型不支持审批.");
        ApprovalFlow? previous = null;
        ApprovalFlowAssociation? association = null;
        var entity = _uow.Create(request);
        if (!string.IsNullOrWhiteSpace(request.AssociatedEntityId))
        {
            List<ApprovalFlow> existing;
            // 构造实例已校验关联对象可访问, 同一对象的历史流程按租户边界查询.
            using (var scope = _uow.DbContext.DisableDataFilters(typeof(IOrgFilteredEntity)))
                existing = _uow.Query<ApprovalFlow>().Where(x => x.Id != entity.Id && x.AssociatedEntityType == request.AssociatedEntityType
                    && x.AssociatedEntityId == entity.AssociatedEntityId && x.Status != ApprovalFlowStatus.Archived).ToList();
            if (existing.Count > 1) throw new UserFriendlyException("该对象存在多个非归档流程, 请先核对关联记录.");
            previous = existing.SingleOrDefault();
            if (previous != null && previous.Status != ApprovalFlowStatus.Canceled)
                throw new UserFriendlyException("该对象已有关联审批流程.");
            var key = $"{request.AssociatedEntityType!.Value}:{entity.AssociatedEntityId}";
            if (!_uow.DbContext.SupportTransaction)
                throw new UserFriendlyException("关联流程需要数据库事务支持.");
            // 每个前序流程只能产生一个替代流程, 唯一索引与归档操作在同一事务内提交.
            association = _uow.Attach(new ApprovalFlowAssociation { Key = key, PreviousFlowId = previous?.Id ?? "" });
        }
        if (association != null)
        {
            association.FlowId = entity.Id;
            previous?.Archive(entity.Id);
        }
        await entity.Start();
        return entity;
    }

    public async Task<ApprovalFlow> EditApprovalFlow(EditApprovalFlowRequest request)
    {
        var entity = _uow.Query<ApprovalFlow>().GetById(request.Id);
        EnsureCreator(entity);
        entity.Edit(request);
        return entity;
    }

    public async Task<bool> CancelApprovalFlow(string id)
    {
        var entity = _uow.Query<ApprovalFlow>().GetById(id);
        EnsureCreator(entity);
        await entity.CancelAsync();
        return true;
    }

    public async Task<bool> FinishApprovalFlow(string id)
    {
        var entity = _uow.Query<ApprovalFlow>().GetById(id);
        EnsureCreator(entity);
        await entity.Finish();
        return true;
    }

    public async Task<bool> MarkNodeAsViewed(string nodeId)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        node.MarkAsViewed();
        return true;
    }

    public async Task<ApprovalFlowNode> ApproveApprovalFlowNode(string nodeId, string message)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.Approve(message);
        return node;
    }

    public async Task<ApprovalFlowNode> WithdrawApprovalFlowNode(string nodeId)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.Withdraw();
        return node;
    }

    public async Task<ApprovalFlowNode> RejectApprovalFlowNode(string nodeId, string message, string targetNodeId)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.BulkReject(message, targetNodeId);
        return node;
    }

    public async Task<ApprovalFlowNode> ConsultApprovalFlowNode(string nodeId, string userId, string message)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.Consult(userId, message);
        return node;
    }

    public async Task<ApprovalFlowNode> TransferApprovalFlowNode(string nodeId, string userId)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.Transfer(userId);
        return node;
    }

    public async Task<ApprovalFlowNode> CarbonCopyApprovalFlowNode(string nodeId, string userId)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.CarbonCopy(userId);
        return node;
    }

    public async Task<ApprovalFlowNode> ReplyApprovalFlowNode(string nodeId, string message)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        await node.Reply(message);
        return node;
    }

    public async Task<bool> MarkApprovalFlowNodeViewed(string nodeId)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(nodeId);
        node.MarkAsViewed();
        return true;
    }

    public async Task<ApprovalFlowTemplate> CreateApprovalFlowTemplate(CreateApprovalFlowTemplateRequest request)
    {
        var entity = _uow.Create(request);
        return entity;
    }

    public async Task<ApprovalFlowTemplate> EditApprovalFlowTemplate(EditApprovalFlowTemplateRequest request)
    {
        var entity = _uow.Query<ApprovalFlowTemplate>().GetById(request.Id);
        entity.Edit(request);
        return entity;
    }

    public async Task<bool> DeleteApprovalFlowTemplate(string[] ids)
    {
        var entities = _uow.Query<ApprovalFlowTemplate>().Where(x => ids.Contains(x.Id));
        await entities.DeleteAsync();
        return true;
    }

    private void EnsureCreator(ApprovalFlow flow)
    {
        if (flow.CreatorUserId != _uow.ServiceProvider.GetRequiredService<ICurrentUser>().UserId)
            throw new UserFriendlyException("仅流程发起人可执行此操作.");
    }
}
