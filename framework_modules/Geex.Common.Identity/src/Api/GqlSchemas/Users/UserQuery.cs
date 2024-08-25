using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Requests;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using Geex.Common.Identity.Core.Aggregates.Users;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Users
{
    public class UserQuery : QueryExtension<UserQuery>
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUser _currentUser;

        public UserQuery(IMediator mediator, ICurrentUser currentUser)
        {
            this._mediator = mediator;
            this._currentUser = currentUser;
        }

        protected override void Configure(IObjectTypeDescriptor<UserQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor.Field(x => x.Users())
            .UseOffsetPaging<ObjectType<User>>()
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
            .UseSorting<IUser>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Id);
                x.Field(y => y.CreatedOn);
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
            var result = await _mediator.Send(new QueryRequest<IUser>());
            return result;
        }

        public async Task<IUser> CurrentUser()
        {
            var userId = _currentUser.UserId;
            var user = (await _mediator.Send(new QueryRequest<IUser>(x => x.Id == userId))).FirstOrDefault();
            return user;
        }
    }
}
