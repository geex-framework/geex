﻿using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;

using Microsoft.AspNetCore.Authorization;

using NetCasbin;

namespace Geex.Common.Authorization.Casbin
{
    public class CasbinAuthorizationHandler : AuthorizationHandler<CasbinRequirement, AuthorizationContext>
    {
        private readonly IRbacEnforcer _enforcer;

        public CasbinAuthorizationHandler(IRbacEnforcer enforcer)
        {
            _enforcer = enforcer;
        }

        /// <summary>
        /// Makes a decision if authorization is allowed based on a specific requirement and resource.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The requirement to evaluate.</param>
        /// <param name="resource">The resource to evaluate.</param>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CasbinRequirement requirement,
            AuthorizationContext resource)
        {
            var mod = requirement.Mod ?? "*"; // the module.
            var act = requirement.Act ?? "*"; // the operation that the user performs on the resource.
            var obj = requirement.Obj ?? "*"; // the resource that is going to be accessed.
            var fields = requirement.Field ?? "*"; // the fields that the user is going to retrieve from the resource.
            var sub = context.User.FindUserId();

            bool result = false;

            if (sub.IsNullOrEmpty())
            {
                sub = "client::" + context.User.FindClientId();
            }

            result = await _enforcer.EnforceAsync(sub, mod, act, obj, fields);

            if (result)
            {
                // permit alice to read data1
                context.Succeed(requirement);
            }
            else
            {
                // deny the request, show an error
                context.Fail();
            }

            return;
        }
    }
}
