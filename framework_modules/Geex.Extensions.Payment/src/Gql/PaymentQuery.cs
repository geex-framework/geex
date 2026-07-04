using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Payment.Requests;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Payment.Gql;

public sealed class PaymentQuery : QueryExtension<PaymentQuery>
{
    private readonly IUnitOfWork _uow;

    public PaymentQuery(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<PaymentQuery> descriptor)
    {
        descriptor.Field(x => x.PaymentOrders())
            .UseOffsetPaging<InterfaceType<IPaymentOrder>>()
            .UseFiltering<IPaymentOrder>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.OutTradeNo);
                x.Field(y => y.BusinessOrderId);
                x.Field(y => y.Subject);
                x.Field(y => y.Provider);
                x.Field(y => y.Channel);
                x.Field(y => y.Status);
            });
        base.Configure(descriptor);
    }

    public async Task<IPaymentOrder?> PaymentOrder(string outTradeNo)
        => await _uow.Request(new GetPaymentOrderRequest(outTradeNo));

    public async Task<IQueryable<IPaymentOrder>> PaymentOrders()
        => await _uow.Request(new QueryRequest<IPaymentOrder>());
}
