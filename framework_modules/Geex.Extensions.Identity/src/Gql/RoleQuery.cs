using System.Linq;
using System.Threading.Tasks;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class RoleQuery : QueryExtension<RoleQuery>
    {

        protected override void Configure(IObjectTypeDescriptor<RoleQuery> descriptor)
        {
            descriptor.Field(x => x.Roles())
            .UseOffsetPaging<InterfaceType<IRole>>()
            .UseFiltering<IRole>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Name);
                x.Field(y => y.Id);
                x.Field(y => y.Users);
            })
            ;
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public RoleQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }
        public async Task<IQueryable<IRole>> Roles(
            )
        {
            return await _uow.Request(new QueryRequest<IRole>());
        }
    }
}
