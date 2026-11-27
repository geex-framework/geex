using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.Favorites;

namespace Geex.Extensions.ApprovalFlows;

public sealed class ApprovalFlowFavoriteEntityType : FavoriteEntityType
{
    private ApprovalFlowFavoriteEntityType() : base(nameof(ApprovalFlow), typeof(ApprovalFlow),
        nameof(Core.Entities.ApprovalFlow.Name), "/approval-flows/inbox/{id}") { }

    public static FavoriteEntityType ApprovalFlow { get; } = new ApprovalFlowFavoriteEntityType();
}
