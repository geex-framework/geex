using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows.Core.EventHandlers;
using Geex.Extensions.ApprovalFlows.Events;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Extensions.Identity;
using Geex.Extensions.Messaging.Core.Entities;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class ApprovalFlowsApiTests : TestsBase
{
    public ApprovalFlowsApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RejectApprovalFlowNodeShouldSendMessagingNotification()
    {
        ApprovalFlowNode node;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.DbContext.DisableDataFilters(typeof(IOrgFilteredEntity), typeof(ITenantFilteredEntity));
            var flow = new ApprovalFlow(new CreateApprovalFlowRequest
            {
                Name = "reject notify test",
                OrgCode = null!,
                Nodes =
                [
                    new ApprovalFlowNodeData
                    {
                        Name = "node1",
                        AuditUserId = GeexConstants.SuperAdminId,
                        Index = 0,
                    },
                ],
            }, uow);
            await uow.SaveChanges();
            node = flow.Nodes.First();
            var handler = scope.ServiceProvider.GetRequiredService<ApprovalFlowNodeEventHandler>();
            await handler.Handle(new ApprovalFlowNodeRejectedEvent(node), CancellationToken.None);
            await uow.SaveChanges();
        }

        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var messages = uow.Query<Message>()
                .Where(x => x.Title.Contains("驳回"))
                .ToList();
            messages.ShouldNotBeEmpty();
            messages.Any(x => x.Title.Contains("reject notify test")).ShouldBeTrue();
        }
    }
}
