using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Storage;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Common.ApprovalFlows
{
    public partial class ApprovalFlowTemplate : Entity<ApprovalFlowTemplate>
    {
        [Obsolete("仅供EF内部使用", true)]
        public ApprovalFlowTemplate()
        {

        }
        public ApprovalFlowTemplate(IApprovalFlowTemplateDate data, IUnitOfWork uow = default)
        {
            this.Name = data.Name;
            this.Description = data.Description;
            this.OrgCode = data.OrgCode;
            this.ApprovalFlowNodeTemplates = data.ApprovalFlowNodeTemplates.Select((x, i) => new ApprovalFlowNodeTemplate(x, i)).ToImmutableList();
            this.ApprovalFlowType = data.ApprovalFlowType;
            this.CreatorUserId = uow.ServiceProvider.GetService<ICurrentUser>()?.UserId;
            uow?.Attach(this);
        }

        public string? CreatorUserId { get; set; }
        public ApprovalFlowType ApprovalFlowType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ImmutableList<ApprovalFlowNodeTemplate> ApprovalFlowNodeTemplates { get; set; } = ImmutableList<ApprovalFlowNodeTemplate>.Empty;
        public string OrgCode { get; set; }

        public void Edit(IApprovalFlowTemplateDate request)
        {
            if (!request.Name.IsNullOrEmpty()) this.Name = request.Name;
            if (!request.Description.IsNullOrEmpty()) this.Description = request.Description;
            if (!request.OrgCode.IsNullOrEmpty()) this.OrgCode = request.OrgCode;
            if (!request.ApprovalFlowNodeTemplates.IsNullOrEmpty())
                this.ApprovalFlowNodeTemplates = request.ApprovalFlowNodeTemplates
                    .Select((x, i) => new ApprovalFlowNodeTemplate(x, i)).ToImmutableList();
            if (request.ApprovalFlowType != default) this.ApprovalFlowType = request.ApprovalFlowType;
        }
    }

    public class ApprovalFlowNodeTemplate
    {
        [Obsolete("仅供EF内部使用", true)]
        public ApprovalFlowNodeTemplate()
        {

        }
        public ApprovalFlowNodeTemplate(IApprovalFlowNodeTemplateData data, int i)
        {
            this.Id = data.Id;
            this.AuditRole = data.AuditRole;
            this.Name = data.Name;
            this.Index = i;
            this.CarbonCopyUserIds = data.CarbonCopyUserIds.ToImmutableList();
        }

        public string Id { get; set; }
        public string AuditRole { get; set; }
        public ImmutableList<string> CarbonCopyUserIds { get; set; } = ImmutableList<string>.Empty;
        public int Index { get; set; }
        public string Name { get; set; }
    }
}
