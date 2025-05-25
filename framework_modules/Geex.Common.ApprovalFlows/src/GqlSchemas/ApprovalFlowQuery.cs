
using System.Linq;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Gql.Types;
using Geex.Common.ApprovalFlows.Requests;

using HotChocolate.Types;

namespace Geex.Common.ApprovalFlows.GqlSchemas
{
    public class ApprovalFlowQuery : QueryExtension<ApprovalFlowQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlowQuery> descriptor)
        {
            descriptor.AuthorizeFieldsImplicitly();
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
                .WhereIf(request.StartTime.HasValue, x => x.CreatedOn >= request.StartTime)
                .WhereIf(request.EndTime.HasValue, x => x.CreatedOn <= request.EndTime)
                .WhereIf(!string.IsNullOrEmpty(request.TemplateId), x => x.TemplateId == request.TemplateId)
                .WhereIf(!string.IsNullOrEmpty(request.CreatorUserId), x => x.CreatorUserId == request.CreatorUserId)
                .WhereIf(request.Status != default, x => x.Status == request.Status);
            return result;
        }

        public ApprovalFlow? ApprovalFlowById(string id) => _uow.Query<ApprovalFlow>().GetById(id);

        public async Task<IQueryable<ApprovalFlowTemplate>> ApprovalFlowTemplate(QueryApprovalFlowTemplateRequest? request)
        {
            request ??= new QueryApprovalFlowTemplateRequest();
            var result = _uow.Query<ApprovalFlowTemplate>()
                .WhereIf(request.StartTime.HasValue, x => x.CreatedOn >= request.StartTime)
                .WhereIf(request.EndTime.HasValue, x => x.CreatedOn <= request.EndTime)
                .WhereIf(!string.IsNullOrEmpty(request.CreatorUserId), x => x.CreatorUserId == request.CreatorUserId)
                .WhereIf(request.OrgCode != default, x => x.OrgCode == request.OrgCode);
            return result;
        }

        public ApprovalFlowTemplate? ApprovalFlowTemplateById(string id) => _uow.Query<ApprovalFlowTemplate>().GetById(id);
    }
}
