using Geex.Extensions.Payments.Events;
using Geex.Extensions.Payments.Requests;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Extensions.Payments.Core.Entities;

public partial class PaymentRefund : Entity<PaymentRefund>, ITenantFilteredEntity, IPaymentRefund
{
    [Obsolete("for internal use only.", false)]
    internal PaymentRefund()
    {
    }

    public PaymentRefund(Payment payment, CreatePaymentRefundRequest request, string refundRequestNo, IUnitOfWork? uow = null)
    {
        uow?.Attach(this);
        PaymentId = payment.Id;
        ClientSn = payment.ClientSn;
        RefundRequestNo = refundRequestNo;
        Amount = request.Amount;
        Status = PaymentRefundStatusEnum.Pending;
    }

    public string PaymentId { get; private set; } = string.Empty;
    public string ClientSn { get; private set; } = string.Empty;
    public string RefundRequestNo { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentRefundStatusEnum Status { get; private set; } = PaymentRefundStatusEnum.Pending;
    public string? TradeNo { get; private set; }
    public string? RefundTradeNo { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string TenantCode { get; set; } = string.Empty;

    public void MarkProcessing()
    {
        if (Status != PaymentRefundStatusEnum.Pending)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Cannot mark processing from status {Status.Name}.");
        Status = PaymentRefundStatusEnum.Processing;
    }

    public void MarkSucceeded(string? refundTradeNo, string? tradeNo = null)
    {
        if (Status == PaymentRefundStatusEnum.Succeeded)
            return;
        Status = PaymentRefundStatusEnum.Succeeded;
        RefundTradeNo = refundTradeNo;
        TradeNo = tradeNo;
        FinishedAt = DateTimeOffset.UtcNow;
        this.AddDomainEvent(new PaymentRefundSucceededEvent(ClientSn, RefundRequestNo, Amount));
    }

    public void MarkFailed()
    {
        if (Status == PaymentRefundStatusEnum.Succeeded)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Cannot mark failed on succeeded refund.");
        Status = PaymentRefundStatusEnum.Failed;
    }

    public void ApplyProviderStatus(PaymentRefundStatusEnum status, string? refundTradeNo = null, string? tradeNo = null)
    {
        if (status == PaymentRefundStatusEnum.Succeeded)
            MarkSucceeded(refundTradeNo, tradeNo);
        else if (status == PaymentRefundStatusEnum.Failed)
            MarkFailed();
        else if (status == PaymentRefundStatusEnum.Processing)
            MarkProcessing();
    }

    public class PaymentRefundBsonConfig : BsonConfig<PaymentRefund>
    {
        protected override void Map(BsonClassMap<PaymentRefund> map, BsonIndexConfig<PaymentRefund> indexConfig)
        {
            map.Inherit<IPaymentRefund>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.RefundRequestNo), options =>
            {
                options.Unique = true;
                options.Background = true;
            });
        }
    }

    public class PaymentRefundGqlConfig : GqlConfig.Object<PaymentRefund>
    {
        protected override void Configure(IObjectTypeDescriptor<PaymentRefund> descriptor)
        {
            descriptor.Implements<InterfaceType<IPaymentRefund>>();
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
        }
    }
}
