using Geex.Extensions.Payments;
using Geex.Extensions.Payments.Core.Jobs;
using Geex.Extensions.Payments.Requests;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class PaymentsServiceTests : TestsBase
{
    public PaymentsServiceTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatePaymentServiceShouldWork()
    {
        using var scope = ScopedService.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var result = await uow.Request(new CreatePaymentRequest
        {
            Amount = 9.99m,
            Subject = "test payment",
            Channel = PaymentChannelEnum.Precreate,
        });
        await uow.SaveChanges();

        result.Payment.ShouldNotBeNull();
        result.Payment.Status.ShouldBe(PaymentStatusEnum.Paying);
        result.Prepay.CodeUrl.ShouldNotBeNullOrWhiteSpace();
        result.Prepay.OutTradeNo.ShouldBe(result.Payment.ClientSn);
    }

    [Fact]
    public async Task QueryPaymentServiceShouldWork()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 1m,
                Subject = "query test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
        }

        using var verifyScope = ScopedService.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var payment = await verifyUow.Request(new GetPaymentRequest(clientSn));
        payment.ShouldNotBeNull();
        payment!.Subject.ShouldBe("query test");
    }

    [Fact]
    public async Task ClosePaymentServiceShouldWork()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 2m,
                Subject = "close test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
        }

        using var closeScope = ScopedService.CreateScope();
        var closeUow = closeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var closed = await closeUow.Request(new ClosePaymentRequest(clientSn));
        await closeUow.SaveChanges();
        closed.Status.ShouldBe(PaymentStatusEnum.Closed);
    }

    [Fact]
    public async Task PaymentCallbackServiceShouldBeIdempotent()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 3m,
                Subject = "callback test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
        }

        await Task.Delay(200);

        using (var callbackScope = ScopedService.CreateScope())
        {
            var callbackUow = callbackScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await callbackUow.Request(new CompletePaymentRequest(clientSn, "tx-1"));
            await callbackUow.SaveChanges();
            await callbackUow.Request(new CompletePaymentRequest(clientSn, "tx-2"));
            await callbackUow.SaveChanges();
        }

        using var verifyScope = ScopedService.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var payment = await verifyUow.Request(new GetPaymentRequest(clientSn));
        payment!.Status.ShouldBe(PaymentStatusEnum.Succeeded);
        payment.TransactionId.ShouldBe("tx-1");
    }

    [Fact]
    public async Task CreateRefundServiceShouldWork()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 10m,
                Subject = "refund test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
            await uow.Request(new CompletePaymentRequest(clientSn, "tx-refund"));
            await uow.SaveChanges();
        }

        using var refundScope = ScopedService.CreateScope();
        var refundUow = refundScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var refund = await refundUow.Request(new CreatePaymentRefundRequest
        {
            ClientSn = clientSn,
            Amount = 4m,
        });
        await refundUow.SaveChanges();

        refund.Status.ShouldBe(PaymentRefundStatusEnum.Succeeded);
        refund.Amount.ShouldBe(4m);
    }

    [Fact]
    public async Task SyncPaymentServiceShouldWork()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 5m,
                Subject = "sync test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
            await uow.Request(new CompletePaymentRequest(clientSn, "tx-sync"));
            await uow.SaveChanges();
        }

        using var syncScope = ScopedService.CreateScope();
        var syncUow = syncScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var payment = await syncUow.Request(new SyncPaymentRequest(clientSn));
        payment.Status.ShouldBe(PaymentStatusEnum.Succeeded);
    }

    [Fact]
    public async Task PaymentShouldBeTenantFiltered()
    {
        var tenantCode = $"tenant-{ObjectId.GenerateNewId()}";
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantCode);
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 4m,
                Subject = "tenant test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
        }

        using var otherScope = ScopedService.CreateScope();
        var otherUow = otherScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        otherUow.ServiceProvider.GetRequiredService<ICurrentTenant>().Change($"other-{ObjectId.GenerateNewId()}");
        var payment = await otherUow.Request(new GetPaymentRequest(clientSn));
        payment.ShouldBeNull();
    }

    [Fact]
    public async Task PaymentTimeoutJobShouldCloseExpired()
    {
        string clientSn;
        DateTimeOffset? expireAt;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 6m,
                Subject = "timeout test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            expireAt = result.Payment.ExpireAt;
            await uow.SaveChanges();
        }

        expireAt.ShouldNotBeNull();
        expireAt.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);

        await Task.Delay(100);

        using var jobScope = ScopedService.CreateScope();
        var job = new PaymentTimeoutJob(jobScope.ServiceProvider, "*/10 * * * * *");
        await job.Run(jobScope.ServiceProvider, CancellationToken.None);

        using var verifyScope = ScopedService.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var payment = await verifyUow.Request(new GetPaymentRequest(clientSn));
        payment!.Status.ShouldBe(PaymentStatusEnum.Closed);
    }
}
