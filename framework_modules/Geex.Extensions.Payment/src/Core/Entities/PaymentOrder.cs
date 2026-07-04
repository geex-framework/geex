using System.Text.Json.Nodes;
using Geex.Extensions.Payment.Events;
using Geex.Extensions.Payment.Requests;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Extensions.Payment.Core.Entities;

public partial class PaymentOrder : Entity<PaymentOrder>, ITenantFilteredEntity, IPaymentOrder
{
    [Obsolete("for internal use only.", false)]
    internal PaymentOrder()
    {
    }

    public PaymentOrder(CreatePaymentOrderRequest request, string outTradeNo, PaymentModuleOptions options, IUnitOfWork? uow = null)
    {
        uow?.Attach(this);
        OutTradeNo = outTradeNo;
        BusinessOrderId = request.BusinessOrderId;
        Provider = request.Provider;
        Channel = request.Channel;
        Status = PaymentStatusEnum.Pending;
        Amount = request.Amount;
        Currency = request.Currency ?? options.DefaultCurrency;
        Subject = request.Subject;
        ExtraData = request.ExtraData;
        ExpireAt = DateTimeOffset.UtcNow.AddMinutes(options.PaymentExpireMinutes);
    }

    public string OutTradeNo { get; private set; } = string.Empty;
    public string? BusinessOrderId { get; private set; }
    public PaymentProviderEnum Provider { get; private set; } = PaymentProviderEnum.Mock;
    public PaymentChannelEnum Channel { get; private set; } = PaymentChannelEnum.Native;
    public PaymentStatusEnum Status { get; private set; } = PaymentStatusEnum.Pending;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public string Subject { get; private set; } = string.Empty;
    public string? PrepayId { get; private set; }
    public string? TransactionId { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ExpireAt { get; private set; }
    public JsonNode? ExtraData { get; private set; }
    public string TenantCode { get; set; } = string.Empty;

    public void MarkPaying(string? prepayId)
    {
        if (Status != PaymentStatusEnum.Pending)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Cannot mark paying from status {Status.Name}.");
        Status = PaymentStatusEnum.Paying;
        PrepayId = prepayId;
    }

    public void MarkSucceeded(string? transactionId)
    {
        if (Status == PaymentStatusEnum.Succeeded)
            return;
        if (Status != PaymentStatusEnum.Pending && Status != PaymentStatusEnum.Paying)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Cannot mark succeeded from status {Status.Name}.");
        Status = PaymentStatusEnum.Succeeded;
        TransactionId = transactionId;
        PaidAt = DateTimeOffset.UtcNow;
        this.AddDomainEvent(new PaymentSucceededEvent(OutTradeNo, BusinessOrderId, Amount, Provider));
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

    public class PaymentOrderBsonConfig : BsonConfig<PaymentOrder>
    {
        protected override void Map(BsonClassMap<PaymentOrder> map, BsonIndexConfig<PaymentOrder> indexConfig)
        {
            map.Inherit<IPaymentOrder>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.OutTradeNo), options =>
            {
                options.Unique = true;
                options.Background = true;
            });
        }
    }

    public class PaymentOrderGqlConfig : GqlConfig.Object<PaymentOrder>
    {
        protected override void Configure(IObjectTypeDescriptor<PaymentOrder> descriptor)
        {
            descriptor.Implements<InterfaceType<IPaymentOrder>>();
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
        }
    }
}
