using System.Threading.Tasks;
using Geex.Extensions.Payments.Requests;
using Geex.Gql.Types;

namespace Geex.Extensions.Payments.Gql;

public sealed class PaymentMutation : MutationExtension<PaymentMutation>
{
    private readonly IUnitOfWork _uow;

    public PaymentMutation(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CreatePaymentResult> CreatePayment(CreatePaymentRequest request)
        => await _uow.Request(request);

    public async Task<IPayment> ClosePayment(ClosePaymentRequest request)
        => await _uow.Request(request);

    public async Task<IPayment> RevokePayment(RevokePaymentRequest request)
        => await _uow.Request(request);

    public async Task<IPayment> SyncPayment(SyncPaymentRequest request)
        => await _uow.Request(request);

    public async Task<IPaymentRefund> CreatePaymentRefund(CreatePaymentRefundRequest request)
        => await _uow.Request(request);

    public async Task<IPaymentRefund> SyncPaymentRefund(SyncPaymentRefundRequest request)
        => await _uow.Request(request);
}
