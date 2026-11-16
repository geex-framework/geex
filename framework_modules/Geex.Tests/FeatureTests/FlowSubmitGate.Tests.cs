using System.Security.Claims;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.ApprovalFlows.Events;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using Geex.MultiTenant;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Tests.FeatureTests;

public interface IFlowSubmitTestEntity : IApproveEntity { }

public class FlowSubmitTestEntity : Entity<FlowSubmitTestEntity>, IFlowSubmitTestEntity, ITenantFilteredEntity, IOrgFilteredEntity
{
    public ApproveStatus ApproveStatus { get; set; }
    public string? ApproveRemark { get; set; }
    public bool CanSubmit { get; set; } = true;
    public bool Submittable => CanSubmit && ApproveStatus == ApproveStatus.Default;
    public string? TenantCode { get; set; }
    public string? OrgCode { get; set; }
}

public class FlowSubmitTestHandler(IUnitOfWork uow) : IApproveRequestHandler<IFlowSubmitTestEntity, FlowSubmitTestEntity>
{
    public IUnitOfWork Uow => uow;
}

[Collection(nameof(TestsCollection))]
public class FlowSubmitGateTests(TestApplicationFactory factory) : TestsBase(factory)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultSubmitAndDefaultHandlerPersistStatusRemarkAndOneEventForRepeatedId(bool viaHandler)
    {
        var id = await Seed();
        const string remark = "提交备注";
        using (var scope = CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            if (viaHandler)
                await ((MediatR.IRequestHandler<SubmitRequest<IFlowSubmitTestEntity>>)new FlowSubmitTestHandler(uow))
                    .Handle(new SubmitRequest<IFlowSubmitTestEntity>(remark, [id, id]), CancellationToken.None);
            else
                await FlowSubmitGate.DefaultSubmit<IFlowSubmitTestEntity, FlowSubmitTestEntity>(uow, [id, id], remark);
            var submitted = Assert.Single(uow.DbContext.DomainEvents.OfType<EntitySubmittedEvent<IFlowSubmitTestEntity>>());
            Assert.Equal(id, submitted.Entity.Id);
            Assert.Equal(ApproveStatus.Submitted, submitted.Entity.ApproveStatus);
            Assert.Equal(remark, submitted.Entity.ApproveRemark);
            await uow.SaveChanges();
        }
        var stored = DB.Collection<FlowSubmitTestEntity>().Find(x => x.Id == id).Single();
        Assert.Equal(ApproveStatus.Submitted, stored.ApproveStatus);
        Assert.Equal(remark, stored.ApproveRemark);
    }

    [Fact]
    public async Task DefaultSubmitPreservesEntitySubmittableValidation()
    {
        var id = await Seed(canSubmit: false);
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var error = await Assert.ThrowsAsync<BusinessException>(() =>
            FlowSubmitGate.DefaultSubmit<IFlowSubmitTestEntity, FlowSubmitTestEntity>(uow, [id], "blocked"));
        Assert.Equal(GeexExceptionType.ValidationFailed.Value, error.ExceptionCode);
        Assert.Empty(uow.DbContext.DomainEvents.OfType<EntitySubmittedEvent<IFlowSubmitTestEntity>>());
        var stored = DB.Collection<FlowSubmitTestEntity>().Find(x => x.Id == id).Single();
        Assert.Equal(ApproveStatus.Default, stored.ApproveStatus);
        Assert.Null(stored.ApproveRemark);
    }

    [Fact]
    public async Task DefaultSubmitRejectsMissingIds()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var error = await Assert.ThrowsAsync<BusinessException>(() =>
            FlowSubmitGate.DefaultSubmit<IFlowSubmitTestEntity, FlowSubmitTestEntity>(uow, [ObjectId.GenerateNewId().ToString()], null));
        Assert.Equal(GeexExceptionType.NotFound.Value, error.ExceptionCode);
        Assert.Empty(uow.DbContext.DomainEvents.OfType<EntitySubmittedEvent<IFlowSubmitTestEntity>>());
    }

    [Fact]
    public async Task DefaultSubmitKeepsTenantAndOrganizationFilters()
    {
        var tenantA = "submit-a-" + Guid.NewGuid().ToString("N");
        var tenantB = "submit-b-" + Guid.NewGuid().ToString("N");
        var foreignTenantId = await Seed(tenantB);
        var foreignOrgId = await Seed(tenantA, "other-org-" + Guid.NewGuid().ToString("N"));
        var accessibleId = await Seed(tenantA);
        using (var scope = CreateScope())
        {
            using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantA);
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var filters = uow.DbContext.DataFilters.ToDictionary(x => x.Key, x => x.Value);
            Assert.Contains(typeof(ITenantFilteredEntity), filters.Keys);
            Assert.Contains(typeof(IOrgFilteredEntity), filters.Keys);
            foreach (var id in new[] { foreignTenantId, foreignOrgId })
            {
                var error = await Assert.ThrowsAsync<BusinessException>(() =>
                    FlowSubmitGate.DefaultSubmit<IFlowSubmitTestEntity, FlowSubmitTestEntity>(uow, [id], null));
                Assert.Equal(GeexExceptionType.NotFound.Value, error.ExceptionCode);
            }
            await FlowSubmitGate.DefaultSubmit<IFlowSubmitTestEntity, FlowSubmitTestEntity>(uow, [accessibleId], "allowed");
            foreach (var entry in filters) Assert.Same(entry.Value, uow.DbContext.DataFilters[entry.Key]);
            await uow.SaveChanges();
        }
        var ids = new[] { foreignTenantId, foreignOrgId, accessibleId };
        var rows = DB.Collection<FlowSubmitTestEntity>()
            .Find(Builders<FlowSubmitTestEntity>.Filter.In(x => x.Id, ids)).ToList();
        Assert.Equal(3, rows.Count);
        Assert.All(rows.Where(x => x.Id != accessibleId), x => Assert.Equal(ApproveStatus.Default, x.ApproveStatus));
        Assert.Equal(ApproveStatus.Submitted, rows.Single(x => x.Id == accessibleId).ApproveStatus);
    }

    private async Task<string> Seed(string? tenantCode = null, string? orgCode = null, bool canSubmit = true)
    {
        using var scope = CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantCode);
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var entity = uow.Attach(new FlowSubmitTestEntity { OrgCode = orgCode, CanSubmit = canSubmit });
        await uow.SaveChanges();
        return entity.Id;
    }

    private IServiceScope CreateScope()
    {
        var scope = ScopedService.CreateScope();
        ((CurrentUser)scope.ServiceProvider.GetRequiredService<ICurrentUser>())._claimsIdentity = new ClaimsIdentity();
        return scope;
    }
}
