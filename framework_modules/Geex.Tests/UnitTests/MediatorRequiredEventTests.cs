using MediatX;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using EventMediator = MediatX.Mediator;

namespace Geex.Tests.UnitTests;

public sealed class MediatorRequiredEventTests
{
    [Fact]
    public async Task Required_publish_propagates_the_original_handler_failure()
    {
        var failure = new InvalidOperationException("Business write failed.");
        var calls = 0;
        using var services = CreateServices(async _ =>
        {
            calls++;
            await Task.Yield();
            throw failure;
        });
        var mediator = CreateMediator(services);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishRequired(new TestEvent("required")));

        Assert.Same(failure, actual);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ordinary_publish_keeps_its_error_policy_after_required_publish(bool requiredFails)
    {
        var ordinaryCalls = 0;
        using var services = CreateServices(async notification =>
        {
            await Task.Yield();
            if (notification.Name == "ordinary") ordinaryCalls++;
            if (notification.Name == "ordinary" || requiredFails)
                throw new InvalidOperationException("Handler failed.");
        });
        var mediator = CreateMediator(services);

        if (requiredFails)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => mediator.PublishRequired(new TestEvent("required")));
        else
            await mediator.PublishRequired(new TestEvent("required"));

        await mediator.Publish(new TestEvent("ordinary"));

        Assert.Equal(1, ordinaryCalls);
    }

    [Fact]
    public async Task Required_publish_propagates_nested_failure_from_another_mediator()
    {
        var failure = new InvalidOperationException("Nested business write failed.");
        using var nestedServices = CreateServices(async _ =>
        {
            await Task.Yield();
            throw failure;
        });
        var nested = CreateMediator(nestedServices);
        using var services = CreateServices(_ => nested.Publish(new TestEvent("nested")));
        var mediator = CreateMediator(services);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishRequired(new TestEvent("outer")));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task Nested_required_publish_preserves_the_outer_failure_policy()
    {
        var failure = new InvalidOperationException("Outer scope must still propagate this failure.");
        var completedNestedCalls = 0;
        using var nestedServices = CreateServices(async notification =>
        {
            await Task.Yield();
            if (notification.Name == "failure") throw failure;
            completedNestedCalls++;
        });
        var nested = CreateMediator(nestedServices);
        using var services = CreateServices(async _ =>
        {
            await nested.PublishRequired(new TestEvent("success"));
            await nested.Publish(new TestEvent("failure"));
        });
        var mediator = CreateMediator(services);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishRequired(new TestEvent("outer")));

        Assert.Same(failure, actual);
        Assert.Equal(1, completedNestedCalls);
    }

    private static ServiceProvider CreateServices(Func<TestEvent, Task> handle)
    {
        var services = new ServiceCollection();
        var handler = new TestEventHandler(handle);
        foreach (var contract in typeof(TestEventHandler).GetInterfaces())
            services.AddSingleton(contract, handler);
        return services.BuildServiceProvider();
    }

    private static EventMediator CreateMediator(IServiceProvider services)
        => new(services, NullLogger<EventMediator>.Instance);

    public sealed record TestEvent(string Name) : IEvent;

    private sealed class TestEventHandler(Func<TestEvent, Task> handle) : IEventHandler<TestEvent>
    {
        public Task Handle(TestEvent notification, CancellationToken cancellationToken) => handle(notification);
    }
}
