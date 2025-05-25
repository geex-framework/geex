using Geex.Storage;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

public partial class ApprovalFlowUserRef : Entity<ApprovalFlowUserRef>
{
    public ApprovalFlowUserRef()
    {

    }

    public ApprovalFlowUserRef(string approvalflowId, string userId, ApprovalFlowOwnershipType ownershipType)
    {
        ApprovalFlowId = approvalflowId;
        UserId = userId;
        OwnershipType = ownershipType;
    }
    public string ApprovalFlowId { get; set; }
    public string UserId { get; set; }
    public ApprovalFlowOwnershipType OwnershipType { get; set; }
}
