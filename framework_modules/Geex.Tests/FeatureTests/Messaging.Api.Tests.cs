using Geex.Extensions.Messaging.Requests;
using Geex.Extensions.Messaging.Core.Sms;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class MessagingApiTests : TestsBase
{
    public MessagingApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SendSmsVirtualShouldWork()
    {
        VirtualSmsStore.Sent.Clear();
        using var scope = ScopedService.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var result = await uow.Request(new SendSmsRequest("13800138000", ["1234"]));
        result.ShouldBeTrue();
        VirtualSmsStore.Sent.Any(x => x.Phone == "13800138000").ShouldBeTrue();
    }

    [Fact]
    public async Task QueryUnreadMessagesShouldWork()
    {
        var client = SuperAdminClient;
        var query = """
            query {
                unreadMessages(take: 10) {
                    items { id title }
                    totalCount
                }
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(query);
        responseString.ShouldNotContain("errors");
        responseData["data"]["unreadMessages"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task DeleteMessageShouldWork()
    {
        string messageId;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var message = await uow.Request(new CreateMessageRequest { Text = "delete me" });
            await uow.SaveChanges();
            messageId = message.Id;
        }

        var client = SuperAdminClient;
        var mutation = """
            mutation($id: String!) {
                deleteMessage(request: { messageId: $id })
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(mutation, new { id = messageId });
        responseString.ShouldNotContain("errors");
        ((bool)responseData["data"]["deleteMessage"]).ShouldBeTrue();
    }
}
