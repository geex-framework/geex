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
    public sealed class UserQuery : QueryExtension<UserQuery>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _currentUser;

        public UserQuery(IUnitOfWork uow, ICurrentUser currentUser)
        {
            this._uow = uow;
            this._currentUser = currentUser;
        }

        protected override void Configure(IObjectTypeDescriptor<UserQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor.Field(x => x.Users())
            .UseOffsetPaging<InterfaceType<IUser>>()
            .UseFiltering<IUser>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Username);
                x.Field(y => y.Nickname);
                x.Field(y => y.IsEnable);
                x.Field(y => y.PhoneNumber);
                x.Field(y => y.CreatedOn);
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
        public async Task<IQueryable<IUser>> Users() => _uow.Query<IUser>();

        public async Task<IUser?> CurrentUser()
        {
            var userId = _currentUser.UserId;
            if (userId == null) return default;
            var user = _uow.Query<IUser>().GetById(userId);
            return user;
        }
    }
}
