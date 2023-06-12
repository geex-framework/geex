using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Api.GqlSchemas.Users.Types;

using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Users
{
    public class UserQuery : QueryExtension<UserQuery>
    {
        private readonly IMediator _mediator;
        private readonly LazyService<ClaimsPrincipal> _claimsPrincipal;

        public UserQuery(IMediator mediator, LazyService<ClaimsPrincipal> claimsPrincipal)
        {
            this._mediator = mediator;
            this._claimsPrincipal = claimsPrincipal;
        }

        protected override void Configure(IObjectTypeDescriptor<UserQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor.Field(x => x.Users())
            .UseOffsetPaging<UserGqlType>()
            .UseFiltering<IUser>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Username);
                x.Field(y => y.Nickname);
                x.Field(y => y.IsEnable);
                x.Field(y => y.PhoneNumber);
                x.Field(y => y.OrgCodes);
                x.Field(y => y.RoleIds);
                x.Field(y => y.Id);
            })
            ;
            base.Configure(descriptor);
        }

        /// <summary>
        /// 列表获取User
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public virtual async Task<IQueryable<IUser>> Users(
            )
        {
            var result = await _mediator.Send(new QueryInput<IUser>());
            return result;
        }

        public async Task<IUser> CurrentUser()
        {
            var userId = _claimsPrincipal.Value.FindUserId();
            var user = (await _mediator.Send(new QueryInput<IUser>(x => x.Id == userId))).FirstOrDefault();
            return user;
        }
    }
}
