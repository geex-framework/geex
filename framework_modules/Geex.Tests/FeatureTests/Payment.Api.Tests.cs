using System.Net;
using System.Text;
using Geex.Extensions.Payment;
using Geex.Extensions.Payment.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class PaymentApiTests : TestsBase
{
    public PaymentApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatePaymentOrderMutationShouldWork()
    {
        var client = SuperAdminClient;
        const string mutation = """
            mutation {
              createPaymentOrder(request: {
                amount: 19.99
                subject: "api payment test"
                provider: Mock
                channel: Native
              }) {
                outTradeNo
                codeUrl
              }
            }
            """;

        var (responseData, _) = await client.PostGqlRequest(mutation);
        var result = responseData["data"]["createPaymentOrder"];
        ((string)result["outTradeNo"]).ShouldNotBeNullOrWhiteSpace();
        ((string)result["codeUrl"]).ShouldContain("mock://pay/native/");
    }

    [Fact]
    public async Task QueryPaymentOrdersShouldWork()
    {
        var client = SuperAdminClient;
        var outTradeNo = $"api-query-{ObjectId.GenerateNewId()}";
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await uow.Request(new CreatePaymentOrderRequest
            {
                Amount = 5m,
                Subject = outTradeNo,
                Provider = PaymentProviderEnum.Mock,
                Channel = PaymentChannelEnum.Native,
            });
            await uow.SaveChanges();
        }

        var query = """
            query($subject: String!) {
              paymentOrders(skip: 0, take: 10, filter: { subject: { eq: $subject } }) {
                items { outTradeNo subject provider status }
                totalCount
              }
            }
            """;
        var (responseData, _) = await client.PostGqlRequest(query, new { subject = outTradeNo });
        var totalCount = responseData["data"]["paymentOrders"]["totalCount"].GetValue<int>();
        totalCount.ShouldBeGreaterThan(0);
        ((string)responseData["data"]["paymentOrders"]["items"][0]["provider"]).ShouldBe("Mock");
    }

    [Fact]
    public async Task PaymentNotifyEndpointShouldWork()
    {
        string outTradeNo;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentOrderRequest
            {
                Amount = 6m,
                Subject = "notify endpoint test",
                Provider = PaymentProviderEnum.Mock,
                Channel = PaymentChannelEnum.Native,
            });
            outTradeNo = result.Order.OutTradeNo;
            await uow.SaveChanges();
        }

        var client = AnonymousClient;
        var payload = $$"""{"outTradeNo":"{{outTradeNo}}","transactionId":"notify-tx-1"}""";
        var response = await client.PostAsync("/payment/notify/wechat", new StringContent(payload, Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var verifyScope = ScopedService.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var order = await verifyUow.Request(new GetPaymentOrderRequest(outTradeNo));
        order!.Status.ShouldBe(PaymentStatusEnum.Succeeded);
        order.TransactionId.ShouldBe("notify-tx-1");
    }
}
