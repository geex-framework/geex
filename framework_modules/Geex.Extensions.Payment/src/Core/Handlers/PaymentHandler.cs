using Geex.Extensions.Payment.Core.Entities;
using Geex.Extensions.Payment.Requests;
using MediatX;
using MongoDB.Bson;
using MongoDB.Entities;

namespace Geex.Extensions.Payment.Core.Handlers;

public class PaymentHandler :
    ICommonHandler<IPaymentOrder, PaymentOrder>,
    IPaymentService,
    IRequestHandler<CreatePaymentOrderRequest, CreatePaymentOrderResult>,
    IRequestHandler<GetPaymentOrderRequest, IPaymentOrder?>,
    IRequestHandler<ClosePaymentOrderRequest, IPaymentOrder>,
    IRequestHandler<CompletePaymentRequest, IPaymentOrder>
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly PaymentModuleOptions _options;

    public PaymentHandler(IUnitOfWork uow, IEnumerable<IPaymentProvider> providers, PaymentModuleOptions options)
    {
        Uow = uow;
        _providers = providers;
        _options = options;
    }

    public IUnitOfWork Uow { get; }

    public async Task<CreatePaymentOrderResult> Handle(CreatePaymentOrderRequest request, CancellationToken cancellationToken)
        => await CreatePaymentOrderAsync(request, cancellationToken);

    public async Task<IPaymentOrder?> Handle(GetPaymentOrderRequest request, CancellationToken cancellationToken)
        => await GetPaymentOrderAsync(request.OutTradeNo, cancellationToken);

    public async Task<IPaymentOrder> Handle(ClosePaymentOrderRequest request, CancellationToken cancellationToken)
        => await ClosePaymentOrderAsync(request.OutTradeNo, cancellationToken);

    public async Task<IPaymentOrder> Handle(CompletePaymentRequest request, CancellationToken cancellationToken)
    {
        await CompletePaymentAsync(request.OutTradeNo, request.TransactionId, cancellationToken);
        return (await GetPaymentOrderAsync(request.OutTradeNo, cancellationToken))!;
    }

    public async Task<CreatePaymentOrderResult> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new BusinessException(GeexExceptionType.OnPurpose, message: "Payment subject is required.");

        var outTradeNo = $"PAY{ObjectId.GenerateNewId()}";
        var order = new PaymentOrder(request, outTradeNo, _options, Uow);
        var provider = ResolveProvider(request.Provider);
        var prepay = await provider.CreatePaymentAsync(order, request.Channel, new PaymentCreateContext
        {
            OpenId = request.OpenId,
            BuyerId = request.BuyerId,
        }, cancellationToken);
        order.MarkPaying(prepay.PrepayId);
        prepay.OutTradeNo = order.OutTradeNo;
        return new CreatePaymentOrderResult { Order = order, Prepay = prepay };
    }

    public Task<IPaymentOrder?> GetPaymentOrderAsync(string outTradeNo, CancellationToken cancellationToken = default)
        => Task.FromResult(Uow.Query<PaymentOrder>().FirstOrDefault(x => x.OutTradeNo == outTradeNo) as IPaymentOrder);

    public async Task<IPaymentOrder> ClosePaymentOrderAsync(string outTradeNo, CancellationToken cancellationToken = default)
    {
        var order = Uow.Query<PaymentOrder>().FirstOrDefault(x => x.OutTradeNo == outTradeNo)
            ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Payment order '{outTradeNo}' not found.");
        order.MarkClosed();
        return order;
    }

    public async Task CompletePaymentAsync(string outTradeNo, string? transactionId, CancellationToken cancellationToken = default)
    {
        var order = Uow.Query<PaymentOrder>().FirstOrDefault(x => x.OutTradeNo == outTradeNo)
            ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Payment order '{outTradeNo}' not found.");
        order.MarkSucceeded(transactionId);
        await Task.CompletedTask;
    }

    private IPaymentProvider ResolveProvider(PaymentProviderEnum provider)
        => _providers.FirstOrDefault(x => x.Provider == provider)
           ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: $"Payment provider '{provider.Name}' is not registered.");
}
