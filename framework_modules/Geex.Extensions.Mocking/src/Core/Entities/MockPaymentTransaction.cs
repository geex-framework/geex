using Geex.Extensions.Payments;
using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockPaymentTransaction : Entity<MockPaymentTransaction>
{
    public MockPaymentTransaction(
        string token,
        string clientSn,
        string origin,
        decimal amount,
        string currency,
        string subject,
        PaymentProviderEnum provider,
        PaymentChannelEnum channel,
        DateTimeOffset expiresAt,
        string? tenantCode = null)
    {
        Token = token;
        ClientSn = clientSn;
        Origin = origin;
        Amount = amount;
        Currency = currency;
        Subject = subject;
        Provider = provider;
        Channel = channel;
        Status = MockPaymentTransactionStatusEnum.Paying;
        ExpiresAt = expiresAt;
        TenantCode = tenantCode;
        TradeNo = $"mock-trade-{clientSn}";
        PrepayId = $"mock-prepay-{clientSn}";
    }

    public string Token { get; private set; }
    public string ClientSn { get; private set; }
    public string Origin { get; private set; }
    public string? TenantCode { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public string Subject { get; private set; }
    public PaymentProviderEnum Provider { get; private set; }
    public PaymentChannelEnum Channel { get; private set; }
    public MockPaymentTransactionStatusEnum Status { get; private set; }
    public string? PrepayId { get; private set; }
    public string? TradeNo { get; private set; }
    public string? TransactionId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool CallbackDispatched { get; private set; }

    public bool IsExpired => DateTimeOffset.Now >= ExpiresAt;

    public void EnsureInteractive()
    {
        if (IsExpired && Status == MockPaymentTransactionStatusEnum.Paying)
        {
            Status = MockPaymentTransactionStatusEnum.Closed;
        }

        if (Status != MockPaymentTransactionStatusEnum.Paying)
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"Payment mock transaction status [{Status}] cannot be updated.");
        }
    }

    public void ConfirmSucceeded(string? transactionId = null)
    {
        EnsureInteractive();
        Status = MockPaymentTransactionStatusEnum.Succeeded;
        TransactionId = transactionId ?? $"mock-tx-{ClientSn}";
        CompletedAt = DateTimeOffset.Now;
    }

    public void ConfirmFailed()
    {
        EnsureInteractive();
        Status = MockPaymentTransactionStatusEnum.Failed;
        CompletedAt = DateTimeOffset.Now;
    }

    public void ConfirmClosed()
    {
        EnsureInteractive();
        Status = MockPaymentTransactionStatusEnum.Closed;
        CompletedAt = DateTimeOffset.Now;
    }

    public void MarkRevoked()
    {
        if (Status != MockPaymentTransactionStatusEnum.Paying && Status != MockPaymentTransactionStatusEnum.Succeeded)
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"Cannot revoke payment mock transaction in status [{Status}].");
        }

        Status = MockPaymentTransactionStatusEnum.Revoked;
        CompletedAt = DateTimeOffset.Now;
    }

    public void MarkCallbackDispatched() => CallbackDispatched = true;

    public PaymentStatusEnum ToPaymentStatus()
    {
        if (Status == MockPaymentTransactionStatusEnum.Paying) return PaymentStatusEnum.Paying;
        if (Status == MockPaymentTransactionStatusEnum.Succeeded) return PaymentStatusEnum.Succeeded;
        if (Status == MockPaymentTransactionStatusEnum.Failed) return PaymentStatusEnum.Failed;
        if (Status == MockPaymentTransactionStatusEnum.Closed) return PaymentStatusEnum.Closed;
        return PaymentStatusEnum.Revoked;
    }

    public override Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(Token) || Token.Length < 22)
        {
            return Task.FromResult(new ValidationResult("Token must be an opaque value of at least 128 bits.", new[] { nameof(Token) }));
        }

        if (string.IsNullOrWhiteSpace(ClientSn))
        {
            return Task.FromResult(new ValidationResult("ClientSn is required.", new[] { nameof(ClientSn) }));
        }

        return Task.FromResult(ValidationResult.Success);
    }
}
