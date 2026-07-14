using Geex.Extensions.Mocking.Core.Entities;
using Geex.Extensions.Mocking.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Mocking.Gql;

public sealed class MockingQuery : QueryExtension<MockingQuery>
{
    private readonly IUnitOfWork _uow;

    public MockingQuery(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<MockingQuery> descriptor)
    {
        descriptor.Field(x => x.MockRules()).Authorize();
        descriptor.Field(x => x.MockWechatProfiles()).Authorize();
        descriptor.Field(x => x.MockSmsMessages(default)).Authorize();
        base.Configure(descriptor);
    }

    public MockingCapabilities MockingCapabilities() => new()
    {
        Enabled = true,
        WechatWeb = true,
        Payments = true,
        Sms = true,
        Management = true,
    };

    public async Task<IQueryable<MockRule>> MockRules()
        => await _uow.Request(new GetMockRulesRequest());

    public async Task<IQueryable<MockWechatProfile>> MockWechatProfiles()
        => await _uow.Request(new GetMockWechatProfilesRequest());

    public async Task<IQueryable<MockSmsMessage>> MockSmsMessages(string? phoneNumber = null)
        => await _uow.Request(new GetMockSmsMessagesRequest { PhoneNumber = phoneNumber });

    public async Task<MockWechatAuthorizationView> MockWechatAuthorizationStatus(string token)
        => await _uow.Request(new GetMockWechatAuthorizationStatusRequest { Token = token });

    public async Task<List<MockWechatProfileSummary>> MockWechatAuthorizeProfiles(string token)
        => await _uow.Request(new GetMockWechatAuthorizeProfilesRequest { Token = token });

    public async Task<MockPaymentTransaction> MockPaymentTransaction(string token)
        => await _uow.Request(new GetMockPaymentTransactionByTokenRequest { Token = token });
}
