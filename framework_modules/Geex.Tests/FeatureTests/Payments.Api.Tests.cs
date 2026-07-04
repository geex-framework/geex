using Geex.Extensions.Payments;
using Geex.Extensions.Payments.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class PaymentsApiTests : TestsBase
{
    public PaymentsApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task QueryPaymentsShouldWork()
    {
        var client = SuperAdminClient;
        var query = """
            query {
                payments(skip: 0, take: 10) {
                    items { id clientSn status amount subject }
                    pageInfo { hasPreviousPage hasNextPage }
                    totalCount
                }
            }
            """;

        var (responseData, _) = await client.PostGqlRequest(query);
        responseData["data"]["payments"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CreatePaymentMutationShouldWork()
    {
        var client = SuperAdminClient;
        var mutation = """
            mutation($request: CreatePaymentRequest!) {
                createPayment(request: $request) {
                    payment { clientSn status amount subject }
                    prepay { outTradeNo codeUrl }
                }
            }
            """;

        var variables = new
        {
            request = new
            {
                amount = 12.34m,
                subject = $"api payment {ObjectId.GenerateNewId()}",
                channel = PaymentChannelEnum.Precreate.Name,
            },
        };

        var (responseData, responseString) = await client.PostGqlRequest(mutation, variables);
        responseString.ShouldNotContain("errors");
        var payment = responseData["data"]["createPayment"]["payment"];
        payment["status"].GetValue<string>().ShouldBe(PaymentStatusEnum.Paying.Name);
        payment["amount"].GetValue<decimal>().ShouldBe(12.34m);
    }

    [Fact]
    public async Task ClosePaymentMutationShouldWork()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 8m,
                Subject = "api close test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
        }

        var client = SuperAdminClient;
        var mutation = """
            mutation($request: ClosePaymentRequest!) {
                closePayment(request: $request) { clientSn status }
            }
            """;

        var (responseData, responseString) = await client.PostGqlRequest(mutation, new { request = new { clientSn } });
        responseString.ShouldNotContain("errors");
        responseData["data"]["closePayment"]["status"].GetValue<string>().ShouldBe(PaymentStatusEnum.Closed.Name);
    }

    [Fact]
    public async Task CreatePaymentRefundMutationShouldWork()
    {
        string clientSn;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new CreatePaymentRequest
            {
                Amount = 20m,
                Subject = "api refund test",
                Channel = PaymentChannelEnum.Precreate,
            });
            clientSn = result.Payment.ClientSn;
            await uow.SaveChanges();
            await uow.Request(new CompletePaymentRequest(clientSn, "tx-api-refund"));
            await uow.SaveChanges();
        }

        var client = SuperAdminClient;
        var mutation = """
            mutation($request: CreatePaymentRefundRequest!) {
                createPaymentRefund(request: $request) { refundRequestNo clientSn amount status }
            }
            """;

        var (responseData, responseString) = await client.PostGqlRequest(mutation, new
        {
            request = new { clientSn, amount = 5m },
        });
        responseString.ShouldNotContain("errors");
        responseData["data"]["createPaymentRefund"]["status"].GetValue<string>().ShouldBe(PaymentRefundStatusEnum.Succeeded.Name);
        responseData["data"]["createPaymentRefund"]["amount"].GetValue<decimal>().ShouldBe(5m);
    }
}
