using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Geex.Extensions.Settings;
using Microsoft.Extensions.DependencyInjection;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.ApprovalFlows.Gql;

public class ApprovalFlowQuery : QueryExtension<ApprovalFlowQuery>
{
    protected override void Configure(IObjectTypeDescriptor<ApprovalFlowQuery> descriptor)
    {
        foreach (var method in typeof(ApprovalFlowQuery).GetMethods(System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            descriptor.Field(method).Authorize();
        descriptor.Field(x => x.ApprovalFlowById(default)).Authorize();
        descriptor.Field(x => x.ApprovalFlowTemplateById(default)).Authorize();
        descriptor.Field(x => x.ApprovalFlow(default))
            .UseOffsetPaging<ObjectType<ApprovalFlow>>()
            .UseFiltering<ApprovalFlow>()
            .UseSorting<ApprovalFlow>();
        descriptor.Field(x => x.ApprovalFlowTemplate(default))
            .UseOffsetPaging<ObjectType<ApprovalFlowTemplate>>()
            .UseFiltering<ApprovalFlowTemplate>()
            .UseSorting<ApprovalFlowTemplate>();
        base.Configure(descriptor);
    }

    private readonly IUnitOfWork _uow;

    public ApprovalFlowQuery(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IQueryable<ApprovalFlow>> ApprovalFlow(QueryApprovalFlowRequest? request)
    {
        request ??= new QueryApprovalFlowRequest();
        var result = _uow.Query<ApprovalFlow>()
            .WhereIf(!string.IsNullOrWhiteSpace(request.Category), x => x.Categories.Contains(request.Category))
            .WhereIf(request.StartTime.HasValue, x => x.CreatedOn >= request.StartTime)
            .WhereIf(request.EndTime.HasValue, x => x.CreatedOn <= request.EndTime)
            .WhereIf(!string.IsNullOrEmpty(request.TemplateId), x => x.TemplateId == request.TemplateId)
            .WhereIf(!string.IsNullOrEmpty(request.CreatorUserId), x => x.CreatorUserId == request.CreatorUserId)
            .WhereIf(request.Status != default, x => x.Status == request.Status);
        return result;
    }

    public ApprovalFlow? ApprovalFlowById(string id) => _uow.Query<ApprovalFlow>().GetById(id);

    public ApprovalFlow? ApprovalFlowByEntity(ApprovalFlowAssociatedEntityType entityType, string entityId)
        => _uow.Query<ApprovalFlow>().SingleOrDefault(x => x.AssociatedEntityType == entityType
            && x.AssociatedEntityId == entityId && x.Status != ApprovalFlowStatus.Archived);

    public async Task<List<string>> ApprovalFlowCategories()
    {
        var setting = await _uow.ServiceProvider.GetRequiredService<ISettingService>().GetSetting(ApprovalFlowSettings.Categories);
        return setting?.Value?.Deserialize<List<string>>() ?? [];
    }

    public async Task<IQueryable<ApprovalFlowTemplate>> ApprovalFlowTemplate(QueryApprovalFlowTemplateRequest? request)
    {
        request ??= new QueryApprovalFlowTemplateRequest();
        var result = _uow.Query<ApprovalFlowTemplate>()
            .WhereIf(!string.IsNullOrWhiteSpace(request.Name), x => x.Name.Contains(request.Name))
            .WhereIf(!string.IsNullOrWhiteSpace(request.Category), x => x.Categories.Contains(request.Category))
            .WhereIf(request.StartTime.HasValue, x => x.CreatedOn >= request.StartTime)
            .WhereIf(request.EndTime.HasValue, x => x.CreatedOn <= request.EndTime)
            .WhereIf(!string.IsNullOrEmpty(request.CreatorUserId), x => x.CreatorUserId == request.CreatorUserId)
            .WhereIf(request.OrgCode != default, x => x.OrgCode == request.OrgCode);
        return result;
    }

    public ApprovalFlowTemplate? ApprovalFlowTemplateById(string id) => _uow.Query<ApprovalFlowTemplate>().GetById(id);
}
