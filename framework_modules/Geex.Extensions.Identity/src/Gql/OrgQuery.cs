using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class OrgQuery : QueryExtension<OrgQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<OrgQuery> descriptor)
        {
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
            descriptor.Field(x => x.OrgsCache()).AllowAnonymous();
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _currentUser;

        public OrgQuery(IUnitOfWork uow, ICurrentUser currentUser)
        {
            this._uow = uow;
            this._currentUser = currentUser;
        }

        public async Task<IQueryable<IOrg>> Orgs()
        {
            var orgs = await _uow.Request(new QueryRequest<IOrg>());
            return orgs.OrderBy(x => x.Code);
        }

        public async Task<IQueryable<IOrg>> OrgsCache()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId))
            {
                return Enumerable.Empty<IOrg>().AsQueryable();
            }

            return await Orgs();
        }
    }
}
