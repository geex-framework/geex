using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class RoleQuery : QueryExtension<RoleQuery>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _currentUser;

        public RoleQuery(IUnitOfWork uow, ICurrentUser currentUser)
        {
            this._uow = uow;
            this._currentUser = currentUser;
        }

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
            descriptor.Field(x => x.RolesCache()).AllowAnonymous();
            base.Configure(descriptor);
        }

        public async Task<IQueryable<IRole>> Roles(
            )
        {
            return await _uow.Request(new QueryRequest<IRole>());
        }

        public async Task<IQueryable<IRole>> RolesCache()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId))
            {
                return Enumerable.Empty<IRole>().AsQueryable();
            }

            return await Roles();
        }
    }
}
