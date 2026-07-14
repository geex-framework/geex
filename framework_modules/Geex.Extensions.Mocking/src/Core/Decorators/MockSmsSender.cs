using System.Text.Json;
using System.Text.Json.Nodes;
using Geex.Extensions.Messaging;
using Geex.Extensions.Mocking.Core.Entities;
using Geex.MultiTenant;
using Geex.Storage;

namespace Geex.Extensions.Mocking.Core.Decorators;

public class MockSmsSender : ISmsSender
{
    private readonly IUnitOfWork _uow;
    private readonly LazyService<ICurrentTenant>? _currentTenant;

    public MockSmsSender(ISmsSender inner, IUnitOfWork uow, LazyService<ICurrentTenant>? currentTenant = null)
    {
        _ = inner;
        _uow = uow;
        _currentTenant = currentTenant;
    }

    public async Task SendAsync(string phoneNumber, IReadOnlyList<string> templateParams, CancellationToken cancellationToken = default)
    {
        var input = new JsonObject
        {
            ["phoneNumber"] = phoneNumber,
            ["templateParams"] = JsonSerializer.SerializeToNode(templateParams),
        };
        var rule = _uow.FindMatchingRule(MockTargetEnum.SmsSender, nameof(SendAsync), input);

        if (rule is null)
        {
            _uow.Attach(new MockSmsMessage(phoneNumber, templateParams, _currentTenant?.Value?.Code, success: true));
            await _uow.SaveChanges(cancellationToken);
            return;
        }

        await rule.ApplyDelayAsync(cancellationToken);
        if (rule.Outcome == MockOutcomeEnum.Throw)
        {
            _uow.Attach(new MockSmsMessage(phoneNumber, templateParams, _currentTenant?.Value?.Code, success: false, errorMessage: rule.Response?.ToJsonString()));
            await _uow.SaveChanges(cancellationToken);
            throw rule.CreateThrowException();
        }

        _uow.Attach(new MockSmsMessage(phoneNumber, templateParams, _currentTenant?.Value?.Code, success: true));
        await _uow.SaveChanges(cancellationToken);
    }
}
