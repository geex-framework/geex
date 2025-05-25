using System.Linq;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Abstractions.Entities;
using Geex.Common.Requests;
using Geex.Abstractions.Gql.Types;
using Geex.Common.Identity.Api.Aggregates.Roles;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles
{
    public sealed class RoleQuery : QueryExtension<RoleQuery>
    {

        protected override void Configure(IObjectTypeDescriptor<RoleQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
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
