using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Payments.Requests;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Payments.Gql;

public sealed class PaymentQuery : QueryExtension<PaymentQuery>
{
    private readonly IUnitOfWork _uow;

    public PaymentQuery(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<PaymentQuery> descriptor)
    {
        descriptor.Field(x => x.Payments())
            .UseOffsetPaging<InterfaceType<IPayment>>()
            .UseFiltering<IPayment>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Id);
                x.Field(y => y.ClientSn);
                x.Field(y => y.Status);
                x.Field(y => y.Provider);
                x.Field(y => y.BusinessOrderId);
            });
        base.Configure(descriptor);
    }

    public async Task<IQueryable<IPayment>> Payments()
        => await _uow.Request(new QueryRequest<IPayment>());

    public async Task<IPayment?> Payment(string clientSn)
        => await _uow.Request(new GetPaymentRequest(clientSn));
}
