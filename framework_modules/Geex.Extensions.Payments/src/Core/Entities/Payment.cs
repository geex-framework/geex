using Geex.Extensions.Payments.Events;
using Geex.Extensions.Payments.Requests;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Extensions.Payments.Core.Entities;

public partial class Payment : Entity<Payment>, ITenantFilteredEntity, IPayment
{
    [Obsolete("for internal use only.", false)]
    internal Payment()
    {
    }

    public Payment(CreatePaymentRequest request, string clientSn, PaymentsModuleOptions options, IUnitOfWork? uow = null)
    {
        uow?.Attach(this);
        ClientSn = clientSn;
        BusinessOrderId = request.BusinessOrderId;
        Provider = request.Provider ?? (options.UseVirtualTransaction ? PaymentProviderEnum.Virtual : PaymentProviderEnum.Shouqianba);
        Channel = request.Channel;
        Status = PaymentStatusEnum.Pending;
        Amount = request.Amount;
        RefundedAmount = 0;
        Currency = options.DefaultCurrency;
        Subject = request.Subject;
        ExpireAt = options.PaymentExpireMinutes <= 0
            ? DateTimeOffset.UtcNow.AddSeconds(-1)
            : DateTimeOffset.UtcNow.AddMinutes(options.PaymentExpireMinutes);
    }

    public string ClientSn { get; private set; } = string.Empty;
    public string? BusinessOrderId { get; private set; }
    public PaymentProviderEnum Provider { get; private set; } = PaymentProviderEnum.Virtual;
    public PaymentChannelEnum Channel { get; private set; } = PaymentChannelEnum.Precreate;
    public PaymentStatusEnum Status { get; private set; } = PaymentStatusEnum.Pending;
    public decimal Amount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public string Subject { get; private set; } = string.Empty;
    public string? PrepayId { get; private set; }
    public string? TradeNo { get; private set; }
    public string? TransactionId { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ExpireAt { get; private set; }
    public string TenantCode { get; set; } = string.Empty;

    public decimal RemainingRefundableAmount => Amount - RefundedAmount;

    public void MarkPaying(string? prepayId, string? tradeNo = null)
    {
        if (Status != PaymentStatusEnum.Pending)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Cannot mark paying from status {Status.Name}.");
        Status = PaymentStatusEnum.Paying;
        PrepayId = prepayId;
        TradeNo = tradeNo;
    }

    public void MarkSucceeded(string? transactionId, string? tradeNo = null)
    {
        if (Status == PaymentStatusEnum.Succeeded)
            return;
        if (Status != PaymentStatusEnum.Pending && Status != PaymentStatusEnum.Paying)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Cannot mark succeeded from status {Status.Name}.");
        Status = PaymentStatusEnum.Succeeded;
        TransactionId = transactionId;
        TradeNo = tradeNo ?? TradeNo;
        PaidAt = DateTimeOffset.UtcNow;
        this.AddDomainEvent(new PaymentSucceededEvent(ClientSn, BusinessOrderId, Amount, Provider));
    }

    public void MarkFailed()
    {
        if (Status == PaymentStatusEnum.Succeeded)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Cannot mark failed on succeeded payment.");
        Status = PaymentStatusEnum.Failed;
    }

    public void MarkClosed()
    {
        if (Status == PaymentStatusEnum.Succeeded)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Cannot close succeeded payment.");
        Status = PaymentStatusEnum.Closed;
    }

    public void MarkRevoked()
    {
        if (Status == PaymentStatusEnum.Succeeded)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Cannot revoke succeeded payment.");
        Status = PaymentStatusEnum.Revoked;
    }

    public void ApplyRefund(decimal refundAmount)
    {
        if (refundAmount <= 0)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Refund amount must be greater than zero.");
        if (RefundedAmount + refundAmount > Amount)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Refund amount exceeds remaining refundable amount.");
        RefundedAmount += refundAmount;
    }

    public void ApplyProviderStatus(PaymentStatusEnum status, string? transactionId = null, string? tradeNo = null)
    {
        if (status == PaymentStatusEnum.Succeeded)
            MarkSucceeded(transactionId, tradeNo);
        else if (status == PaymentStatusEnum.Failed)
            MarkFailed();
        else if (status == PaymentStatusEnum.Closed)
            MarkClosed();
        else if (status == PaymentStatusEnum.Revoked)
            MarkRevoked();
        else if (status == PaymentStatusEnum.Paying)
            MarkPaying(PrepayId, tradeNo);
    }

    public class PaymentBsonConfig : BsonConfig<Payment>
    {
        protected override void Map(BsonClassMap<Payment> map, BsonIndexConfig<Payment> indexConfig)
        {
            map.Inherit<IPayment>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.ClientSn), options =>
            {
                options.Unique = true;
                options.Background = true;
            });
        }
    }

    public class PaymentGqlConfig : GqlConfig.Object<Payment>
    {
        protected override void Configure(IObjectTypeDescriptor<Payment> descriptor)
        {
            descriptor.Implements<InterfaceType<IPayment>>();
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
        }
    }
}
