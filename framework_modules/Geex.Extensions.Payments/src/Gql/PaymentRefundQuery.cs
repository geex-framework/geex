using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Payments.Requests;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Payments.Gql;

public sealed class PaymentRefundQuery : QueryExtension<PaymentRefundQuery>
{
    private readonly IUnitOfWork _uow;

    public PaymentRefundQuery(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<PaymentRefundQuery> descriptor)
    {
        descriptor.Field(x => x.PaymentRefunds())
            .UseOffsetPaging<InterfaceType<IPaymentRefund>>()
            .UseFiltering<IPaymentRefund>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Id);
                x.Field(y => y.ClientSn);
                x.Field(y => y.RefundRequestNo);
                x.Field(y => y.Status);
            });
        base.Configure(descriptor);
    }

    public async Task<IQueryable<IPaymentRefund>> PaymentRefunds()
        => await _uow.Request(new QueryRequest<IPaymentRefund>());

    public async Task<IPaymentRefund?> PaymentRefund(string refundRequestNo)
        => await _uow.Request(new GetPaymentRefundRequest(refundRequestNo));
}
