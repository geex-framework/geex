using Geex.Extensions.Mocking.Core.Entities;
using Geex.Extensions.Mocking.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Mocking.Gql;

public sealed class MockingMutation : MutationExtension<MockingMutation>
{
    private readonly IUnitOfWork _uow;

    public MockingMutation(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<MockingMutation> descriptor)
    {
        descriptor.Field(x => x.CreateMockRule(default!)).Authorize();
        descriptor.Field(x => x.EditMockRule(default!)).Authorize();
        descriptor.Field(x => x.DeleteMockRule(default!)).Authorize();
        descriptor.Field(x => x.SetMockRuleEnabled(default!)).Authorize();
        descriptor.Field(x => x.CreateMockWechatProfile(default!)).Authorize();
        descriptor.Field(x => x.EditMockWechatProfile(default!)).Authorize();
        descriptor.Field(x => x.DeleteMockWechatProfile(default!)).Authorize();
        descriptor.Field(x => x.ClearMockSmsMessages(default!)).Authorize();
        base.Configure(descriptor);
    }

    public async Task<MockRule> CreateMockRule(CreateMockRuleRequest request) => await _uow.Request(request);
    public async Task<MockRule> EditMockRule(EditMockRuleRequest request) => await _uow.Request(request);
    public async Task<bool> DeleteMockRule(DeleteMockRuleRequest request) => await _uow.Request(request);
    public async Task<MockRule> SetMockRuleEnabled(SetMockRuleEnabledRequest request) => await _uow.Request(request);

    public async Task<MockWechatProfile> CreateMockWechatProfile(CreateMockWechatProfileRequest request) => await _uow.Request(request);
    public async Task<MockWechatProfile> EditMockWechatProfile(EditMockWechatProfileRequest request) => await _uow.Request(request);
    public async Task<bool> DeleteMockWechatProfile(DeleteMockWechatProfileRequest request) => await _uow.Request(request);

    public async Task<MockWechatAuthorizationView> CreateMockWechatAuthorization(CreateMockWechatAuthorizationRequest request)
        => await _uow.Request(request);

    public async Task<MockWechatAuthorizationView> ConfirmMockWechatAuthorization(ConfirmMockWechatAuthorizationRequest request)
        => await _uow.Request(request);

    public async Task<MockPaymentTransaction> ConfirmMockPaymentTransaction(ConfirmMockPaymentTransactionRequest request)
        => await _uow.Request(request);

    public async Task<bool> ClearMockSmsMessages(ClearMockSmsMessagesRequest request) => await _uow.Request(request);
}
