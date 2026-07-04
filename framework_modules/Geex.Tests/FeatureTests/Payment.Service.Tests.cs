using Geex.Extensions.Payment;
using Geex.Extensions.Payment.Requests;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class PaymentServiceTests : TestsBase
{
    public PaymentServiceTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatePaymentOrderServiceShouldWork()
    {
        using var scope = ScopedService.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var result = await uow.Request(new CreatePaymentOrderRequest
        {
            Amount = 9.99m,
            Subject = "test payment",
            Provider = PaymentProviderEnum.Mock,
            Channel = PaymentChannelEnum.Native,
        });
        await uow.SaveChanges();

        result.Order.ShouldNotBeNull();
        result.Order.Status.ShouldBe(PaymentStatusEnum.Paying);
        result.Prepay.CodeUrl.ShouldNotBeNullOrWhiteSpace();
        result.Prepay.OutTradeNo.ShouldBe(result.Order.OutTradeNo);
    }

    [Fact]
    public async Task QueryPaymentOrderServiceShouldWork()
    {
        string outTradeNo;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentOrderRequest
            {
                Amount = 1m,
                Subject = "query test",
                Provider = PaymentProviderEnum.Mock,
                Channel = PaymentChannelEnum.Native,
            });
            outTradeNo = result.Order.OutTradeNo;
            await uow.SaveChanges();
        }

        using var verifyScope = ScopedService.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var order = await verifyUow.Request(new GetPaymentOrderRequest(outTradeNo));
        order.ShouldNotBeNull();
        order!.Subject.ShouldBe("query test");
    }

    [Fact]
    public async Task ClosePaymentOrderServiceShouldWork()
    {
        string outTradeNo;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentOrderRequest
            {
                Amount = 2m,
                Subject = "close test",
                Provider = PaymentProviderEnum.Mock,
                Channel = PaymentChannelEnum.Native,
            });
            outTradeNo = result.Order.OutTradeNo;
            await uow.SaveChanges();
        }

        using var closeScope = ScopedService.CreateScope();
        var closeUow = closeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var closed = await closeUow.Request(new ClosePaymentOrderRequest(outTradeNo));
        await closeUow.SaveChanges();
        closed.Status.ShouldBe(PaymentStatusEnum.Closed);
    }

    [Fact]
    public async Task PaymentCallbackServiceShouldBeIdempotent()
    {
        string outTradeNo;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentOrderRequest
            {
                Amount = 3m,
                Subject = "callback test",
                Provider = PaymentProviderEnum.Mock,
                Channel = PaymentChannelEnum.Native,
            });
            outTradeNo = result.Order.OutTradeNo;
            await uow.SaveChanges();
        }

        using (var callbackScope = ScopedService.CreateScope())
        {
            var callbackUow = callbackScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await callbackUow.Request(new CompletePaymentRequest(outTradeNo, "tx-1"));
            await callbackUow.SaveChanges();
            await callbackUow.Request(new CompletePaymentRequest(outTradeNo, "tx-2"));
            await callbackUow.SaveChanges();
        }

        using var verifyScope = ScopedService.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var order = await verifyUow.Request(new GetPaymentOrderRequest(outTradeNo));
        order!.Status.ShouldBe(PaymentStatusEnum.Succeeded);
        order.TransactionId.ShouldBe("tx-1");
    }

    [Fact]
    public async Task PaymentOrderShouldBeTenantFiltered()
    {
        var tenantCode = $"tenant-{ObjectId.GenerateNewId()}";
        string outTradeNo;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantCode);
            var result = await uow.Request(new CreatePaymentOrderRequest
            {
                Amount = 4m,
                Subject = "tenant test",
                Provider = PaymentProviderEnum.Mock,
                Channel = PaymentChannelEnum.Native,
            });
            outTradeNo = result.Order.OutTradeNo;
            await uow.SaveChanges();
        }

        using (var otherScope = ScopedService.CreateScope())
        {
            var otherUow = otherScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            otherUow.ServiceProvider.GetRequiredService<ICurrentTenant>().Change($"other-{ObjectId.GenerateNewId()}");
            var order = await otherUow.Request(new GetPaymentOrderRequest(outTradeNo));
            order.ShouldBeNull();
        }
    }
}
