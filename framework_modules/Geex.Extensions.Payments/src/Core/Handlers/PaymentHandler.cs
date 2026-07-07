using Geex.Extensions.Payments.Core.Entities;
using Geex.Extensions.Payments.Infrastructure;
using Geex.Extensions.Payments.Requests;
using MediatX;
using MongoDB.Bson;

namespace Geex.Extensions.Payments.Core.Handlers;

public class PaymentHandler :
    ICommonHandler<IPayment, Payment>,
    ICommonHandler<IPaymentRefund, PaymentRefund>,
    IRequestHandler<CreatePaymentRequest, CreatePaymentResult>,
    IRequestHandler<GetPaymentRequest, IPayment?>,
    IRequestHandler<GetPaymentRefundRequest, IPaymentRefund?>,
    IRequestHandler<ClosePaymentRequest, IPayment>,
    IRequestHandler<RevokePaymentRequest, IPayment>,
    IRequestHandler<SyncPaymentRequest, IPayment>,
    IRequestHandler<CompletePaymentRequest, IPayment>,
    IRequestHandler<CreatePaymentRefundRequest, IPaymentRefund>,
    IRequestHandler<SyncPaymentRefundRequest, IPaymentRefund>,
    IRequestHandler<CompletePaymentRefundRequest, IPaymentRefund>
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly PaymentsModuleOptions _options;
    private readonly PaymentNotifyUrlResolver _notifyUrlResolver;

    public PaymentHandler(
        IUnitOfWork uow,
        IEnumerable<IPaymentProvider> providers,
        PaymentsModuleOptions options,
        PaymentNotifyUrlResolver notifyUrlResolver)
    {
        Uow = uow;
        _providers = providers;
        _options = options;
        _notifyUrlResolver = notifyUrlResolver;
    }

    public IUnitOfWork Uow { get; }

    public async Task<CreatePaymentResult> Handle(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Payment subject is required.");

        var clientSn = $"PAY{ObjectId.GenerateNewId()}";
        var payment = new Payment(request, clientSn, _options, Uow);
        var provider = ResolveProvider(payment.Provider);
        var prepay = await provider.CreatePaymentAsync(payment, request.Channel, new PaymentCreateContext
        {
            AuthCode = request.AuthCode,
            NotifyUrl = _notifyUrlResolver.GetPaymentNotifyUrl(),
            RefundNotifyUrl = _notifyUrlResolver.GetRefundNotifyUrl(),
        }, cancellationToken);
        payment.MarkPaying(prepay.PrepayId, prepay.TradeNo);
        prepay.OutTradeNo = payment.ClientSn;
        return new CreatePaymentResult(payment, prepay);
    }

    public Task<IPayment?> Handle(GetPaymentRequest request, CancellationToken cancellationToken)
        => Task.FromResult(Uow.Query<Payment>().FirstOrDefault(x => x.ClientSn == request.ClientSn) as IPayment);

    public Task<IPaymentRefund?> Handle(GetPaymentRefundRequest request, CancellationToken cancellationToken)
        => Task.FromResult(Uow.Query<PaymentRefund>().FirstOrDefault(x => x.RefundRequestNo == request.RefundRequestNo) as IPaymentRefund);

    public async Task<IPayment> Handle(ClosePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = GetRequiredPayment(request.ClientSn);
        var provider = ResolveProvider(payment.Provider);
        var result = await provider.CancelPaymentAsync(payment, cancellationToken);
        if (!result.Success)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: result.Message ?? "Cancel payment failed.");
        payment.MarkClosed();
        return payment;
    }

    public async Task<IPayment> Handle(RevokePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = GetRequiredPayment(request.ClientSn);
        var provider = ResolveProvider(payment.Provider);
        var result = await provider.RevokePaymentAsync(payment, cancellationToken);
        if (!result.Success)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: result.Message ?? "Revoke payment failed.");
        payment.MarkRevoked();
        return payment;
    }

    public async Task<IPayment> Handle(SyncPaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = GetRequiredPayment(request.ClientSn);
        var provider = ResolveProvider(payment.Provider);
        var query = await provider.QueryPaymentAsync(payment, cancellationToken);
        payment.ApplyProviderStatus(query.Status, query.TransactionId, query.TradeNo);
        return payment;
    }

    public async Task<IPayment> Handle(CompletePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = GetRequiredPayment(request.ClientSn);
        payment.MarkSucceeded(request.TransactionId);
        return payment;
    }

    public async Task<IPaymentRefund> Handle(CreatePaymentRefundRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Refund amount must be greater than zero.");

        var payment = GetRequiredPayment(request.ClientSn);
        if (payment.Status != PaymentStatusEnum.Succeeded)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Only succeeded payments can be refunded.");
        if (request.Amount > payment.RemainingRefundableAmount)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Refund amount exceeds remaining refundable amount.");

        var refundRequestNo = request.RefundRequestNo ?? $"REF{ObjectId.GenerateNewId()}";
        var refund = new PaymentRefund(payment, request, refundRequestNo, Uow);
        var provider = ResolveProvider(payment.Provider);
        refund.MarkProcessing();
        var result = await provider.RefundAsync(payment, refund, cancellationToken);
        if (!result.Success)
        {
            refund.MarkFailed();
            throw new BusinessException(GeexExceptionType.OnPurpose, message: result.Message ?? "Refund failed.");
        }

        payment.ApplyRefund(refund.Amount);
        if (result.RefundTradeNo is not null)
            refund.MarkSucceeded(result.RefundTradeNo, result.TradeNo);
        return refund;
    }

    public async Task<IPaymentRefund> Handle(SyncPaymentRefundRequest request, CancellationToken cancellationToken)
    {
        var refund = GetRequiredRefund(request.RefundRequestNo);
        var payment = GetRequiredPayment(refund.ClientSn);
        var provider = ResolveProvider(payment.Provider);
        var query = await provider.QueryRefundAsync(payment, refund, cancellationToken);
        refund.ApplyProviderStatus(query.Status, query.RefundTradeNo, query.TradeNo);
        return refund;
    }

    public async Task<IPaymentRefund> Handle(CompletePaymentRefundRequest request, CancellationToken cancellationToken)
    {
        var refund = GetRequiredRefund(request.RefundRequestNo);
        if (refund.Status != PaymentRefundStatusEnum.Succeeded)
            refund.MarkSucceeded(request.RefundTradeNo);
        return refund;
    }

    private Payment GetRequiredPayment(string clientSn)
        => Uow.Query<Payment>().FirstOrDefault(x => x.ClientSn == clientSn)
           ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Payment '{clientSn}' not found.");

    private PaymentRefund GetRequiredRefund(string refundRequestNo)
        => Uow.Query<PaymentRefund>().FirstOrDefault(x => x.RefundRequestNo == refundRequestNo)
           ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Payment refund '{refundRequestNo}' not found.");

    private IPaymentProvider ResolveProvider(PaymentProviderEnum provider)
        => _providers.FirstOrDefault(x => x.Provider == provider)
           ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Payment provider '{provider.Name}' is not registered.");
}
