using Geex.Extensions.BackgroundJob;
using Geex.Extensions.Payments.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Payments.Core.Tasks;

public record VirtualPaymentCallbackParam(string ClientSn, string TransactionId);

public class VirtualPaymentCallbackTask : FireAndForgetTask<VirtualPaymentCallbackParam>
{
    public VirtualPaymentCallbackTask(VirtualPaymentCallbackParam param) : base(param)
    {
    }

    public override async Task Run(CancellationToken token)
    {
        using var scope = ServiceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.Request(new CompletePaymentRequest(Param.ClientSn, Param.TransactionId), token);
        await uow.SaveChanges(token);
    }
}

public record VirtualRefundCallbackParam(string RefundRequestNo, string? RefundTradeNo);

public class VirtualRefundCallbackTask : FireAndForgetTask<VirtualRefundCallbackParam>
{
    public VirtualRefundCallbackTask(VirtualRefundCallbackParam param) : base(param)
    {
    }

    public override async Task Run(CancellationToken token)
    {
        using var scope = ServiceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.Request(new CompletePaymentRefundRequest(Param.RefundRequestNo, Param.RefundTradeNo), token);
        await uow.SaveChanges(token);
    }
}
