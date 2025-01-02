using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.ApprovalFlows.Requests;

using HotChocolate.Types;

namespace Geex.Common.ApprovalFlows.GqlSchemas
{
    public class ApprovalFlowMutation : MutationExtension<ApprovalFlowMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlowMutation> descriptor)
        {
            descriptor.AuthorizeFieldsImplicitly();
            base.Configure(descriptor);
        }

        private IUnitOfWork _uow;

        public ApprovalFlowMutation(IUnitOfWork uow) => _uow = uow;

        public async Task<ApprovalFlow> CreateApprovalFlow(CreateApprovalFlowRequest request)
        {
            var entity = new ApprovalFlow(request);
            _uow.Attach(entity);
            return entity;
        }

        public async Task<ApprovalFlow> EditApprovalFlow(EditApprovalFlowRequest request)
        {
            var entity = _uow.Query<ApprovalFlow>().GetById(request.Id);
            if (entity.CanEdit)
            {
                entity.Name = request.Name;
                entity.Description = request.Description;
                entity.Nodes = request.ApprovalFlowNodes.Select((x, i) => _uow.Create(x)).ToImmutableList();
            }
            return entity;
        }

        public async Task<bool> CancelApprovalFlow(string id)
        {
            var entity = _uow.Query<ApprovalFlow>().GetById(id);
            await entity.CancelAsync();
            return true;
        }

        public async Task<bool> FinishApprovalFlow(string id)
        {
            var entity = _uow.Query<ApprovalFlow>().GetById(id);
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
            var entity = new ApprovalFlowTemplate(request, _uow);
            _uow.Attach(entity);
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
    }
}
