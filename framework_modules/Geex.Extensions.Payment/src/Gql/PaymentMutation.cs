using System.Threading.Tasks;
using Geex.Extensions.Payment.Requests;
using Geex.Gql.Types;

namespace Geex.Extensions.Payment.Gql;

public sealed class PaymentMutation : MutationExtension<PaymentMutation>
{
    private readonly IUnitOfWork _uow;

    public PaymentMutation(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PaymentPrepayResult> CreatePaymentOrder(CreatePaymentOrderRequest request)
    {
        var result = await _uow.Request(request);
        return result.Prepay;
    }

    public async Task<IPaymentOrder> ClosePaymentOrder(string outTradeNo)
        => await _uow.Request(new ClosePaymentOrderRequest(outTradeNo));
}
