using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Payments.Extensions;

internal static class PaymentsEndpointExtensions
{
    public static void UsePaymentsNotify(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<PaymentsModuleOptions>();
        var providers = endpoints.ServiceProvider.GetServices<IPaymentProvider>().ToDictionary(x => x.Provider);

        endpoints.Map(options.ShouqianbaNotifyPath, async context =>
        {
            var provider = ResolveNotifyProvider(providers, options);
            await HandleNotifyAsync(context, ct => provider.HandlePaymentNotifyAsync(context.Request, ct));
        });

        endpoints.Map(options.ShouqianbaRefundNotifyPath, async context =>
        {
            var provider = ResolveNotifyProvider(providers, options);
            await HandleNotifyAsync(context, ct => provider.HandleRefundNotifyAsync(context.Request, ct));
        });
    }

    private static IPaymentProvider ResolveNotifyProvider(IReadOnlyDictionary<PaymentProviderEnum, IPaymentProvider> providers, PaymentsModuleOptions options)
    {
        if (options.UseVirtualTransaction && providers.TryGetValue(PaymentProviderEnum.Virtual, out var virtualProvider))
            return virtualProvider;
        return providers.GetValueOrDefault(PaymentProviderEnum.Shouqianba)
               ?? providers.Values.First();
    }

    private static async Task HandleNotifyAsync(HttpContext context, Func<CancellationToken, Task<PaymentCallbackResult>> handler)
    {
        var response = context.Response;
        try
        {
            var result = await handler(context.RequestAborted);
            response.ContentType = result.ContentType;
            response.StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
            await response.WriteAsync(result.ResponseBody);
        }
        finally
        {
            if (!response.HasStarted)
                await response.StartAsync();
            await response.CompleteAsync();
        }
    }
}
