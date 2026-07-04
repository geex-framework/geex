using Geex.Extensions.Payment.Core.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Payment.Extensions;

internal static class PaymentEndpointExtensions
{
    public static void UsePaymentNotify(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<PaymentModuleOptions>();
        var providers = endpoints.ServiceProvider.GetServices<IPaymentProvider>().ToDictionary(x => x.Provider);

        endpoints.Map(options.WeChatNotifyPath, async context =>
        {
            await HandleNotifyAsync(context, providers.GetValueOrDefault(PaymentProviderEnum.Mock) ?? providers.GetValueOrDefault(PaymentProviderEnum.WeChatPay));
        });

        endpoints.Map(options.AlipayNotifyPath, async context =>
        {
            await HandleNotifyAsync(context, providers.GetValueOrDefault(PaymentProviderEnum.Mock) ?? providers.GetValueOrDefault(PaymentProviderEnum.Alipay));
        });
    }

    private static async Task HandleNotifyAsync(HttpContext context, IPaymentProvider? provider)
    {
        var response = context.Response;
        try
        {
            if (provider is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var result = await provider.HandleCallbackAsync(context.Request);
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
