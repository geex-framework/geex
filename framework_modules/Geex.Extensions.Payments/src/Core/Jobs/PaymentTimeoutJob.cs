using Geex.Extensions.Payments.Core.Entities;
using Geex.Extensions.BackgroundJob.Core;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;
namespace Geex.Extensions.Payments.Core.Jobs;

public class PaymentTimeoutJob : CronJob<PaymentTimeoutJob>
{
    public PaymentTimeoutJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
    {
    }

    public override bool IsConcurrentAllowed => false;

    public override async Task Run(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var _ = uow.As<GeexDbContext>().DisableAllDataFilters();
        var now = DateTimeOffset.UtcNow;
        var expiredPayments = uow.Query<Payment>()
            .Where(x => x.Status == PaymentStatusEnum.Pending || x.Status == PaymentStatusEnum.Paying)
            .ToList()
            .Where(x => x.ExpireAt != null && x.ExpireAt <= now)
            .ToList();

        foreach (var payment in expiredPayments)
        {
            uow.Attach(payment);
            payment.MarkClosed();
        }

        if (expiredPayments.Count > 0)
            await uow.SaveChanges(stoppingToken);
    }
}
