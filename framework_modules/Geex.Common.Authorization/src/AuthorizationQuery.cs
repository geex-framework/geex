﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace Geex.Common.Authorization
{
    public sealed class AuthorizationQuery : QueryExtension<AuthorizationQuery>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IMediator _mediator;

        public AuthorizationQuery(ICurrentUser currentUser,IMediator mediator)
        {
            _currentUser = currentUser;
            _mediator = mediator;
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuthorizationQuery> descriptor)
        {
            base.Configure(descriptor);
        }

        public async Task<List<string>> MyPermissions(
            [Service] IRbacEnforcer enforcer)
        {
            var myId = _currentUser?.UserId;
            return _mediator.Send(new GetSubjectPermissionsRequest(myId)).Result.ToList();
        }
    }
}
