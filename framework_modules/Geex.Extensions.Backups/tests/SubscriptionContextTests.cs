using System.Reflection;
using System.Security.Claims;
using Geex.Extensions.Authentication.Core.Utils;
using HotChocolate;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public class SubscriptionContextTests
{
    [Fact]
    public async Task AuthenticatedSubscriptionReceivesAuthorizationAndRequestContext()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            RequestAborted = cancellation.Token,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "backup-reader")], "Test"))
        };
        var connection = SocketProxy.For<ISocketConnection>(new()
        {
            ["get_HttpContext"] = context,
            ["get_RequestServices"] = services,
            ["get_RequestAborted"] = cancellation.Token
        });
        var session = SocketProxy.For<ISocketSession>(new() { ["get_Connection"] = connection });
        var builder = QueryRequestBuilder.New().SetQuery("subscription { onBackupsChanged }");
        var interceptor = new SubscriptionAuthInterceptor(null!, null!, null!);

        await interceptor.OnRequestAsync(session, "backup-events", builder);

        var request = builder.Create();
        Assert.Same(services, request.Services);
        Assert.Same(context.User, request.ContextData![nameof(ClaimsPrincipal)]);
        Assert.Same(context, request.ContextData[nameof(HttpContext)]);
        Assert.Same(session, request.ContextData[nameof(ISocketSession)]);
        Assert.Equal(cancellation.Token, request.ContextData[nameof(CancellationToken)]);
        Assert.NotNull(request.ContextData[WellKnownContextData.UserState]);
    }

    public class SocketProxy : DispatchProxy
    {
        private Dictionary<string, object> _getters = new();

        public static T For<T>(Dictionary<string, object> getters) where T : class
        {
            var instance = Create<T, SocketProxy>();
            ((SocketProxy)(object)instance)._getters = getters;
            return instance;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _getters.TryGetValue(targetMethod!.Name, out var value) ? value : throw new NotSupportedException(targetMethod.Name);
    }
}
