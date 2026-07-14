using Geex.Extensions.Payments;
using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockPaymentRefund : Entity<MockPaymentRefund>
{
    public MockPaymentRefund(string paymentClientSn, string refundRequestNo, decimal amount, string? tradeNo = null)
    {
        PaymentClientSn = paymentClientSn;
        RefundRequestNo = refundRequestNo;
        Amount = amount;
        TradeNo = tradeNo;
        Status = PaymentRefundStatusEnum.Processing;
        RefundTradeNo = $"mock-refund-{refundRequestNo}";
    }

    public string PaymentClientSn { get; private set; }
    public string RefundRequestNo { get; private set; }
    public decimal Amount { get; private set; }
    public string? TradeNo { get; private set; }
    public string? RefundTradeNo { get; private set; }
    public PaymentRefundStatusEnum Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkSucceeded(string? refundTradeNo = null)
    {
        Status = PaymentRefundStatusEnum.Succeeded;
        RefundTradeNo = refundTradeNo ?? RefundTradeNo ?? $"mock-refund-{RefundRequestNo}";
        CompletedAt = DateTimeOffset.Now;
    }

    public void MarkFailed()
    {
        Status = PaymentRefundStatusEnum.Failed;
        CompletedAt = DateTimeOffset.Now;
    }

    public override Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(RefundRequestNo))
        {
            return Task.FromResult(new ValidationResult("RefundRequestNo is required.", new[] { nameof(RefundRequestNo) }));
        }

        return Task.FromResult(ValidationResult.Success);
    }
}
