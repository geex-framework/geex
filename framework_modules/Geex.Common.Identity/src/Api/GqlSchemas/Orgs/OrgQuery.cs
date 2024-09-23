using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Requests;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Orgs
{
    public sealed class OrgQuery : QueryExtension<OrgQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<OrgQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor
                .Field(x => x.Orgs())
                .UseOffsetPaging<InterfaceType<IOrg>>()
                .UseFiltering<IOrg>(x =>
                {
                    x.BindFieldsExplicitly();
                    x.Field(y => y.Name);
                    x.Field(y => y.Code);
                    x.Field(y => y.ParentOrgCode);
                    x.Field(y => y.OrgType);
                })
            ;
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public OrgQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<IQueryable<IOrg>> Orgs()
        {
            var orgs = await _uow.Request(new QueryRequest<IOrg>());
            return orgs.OrderBy(x => x.Code);
        }
    }
}
