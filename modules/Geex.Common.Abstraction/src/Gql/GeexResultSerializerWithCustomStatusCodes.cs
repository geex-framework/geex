using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using HotChocolate;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexResultSerializerWithCustomStatusCodes : DefaultHttpResponseFormatter
    {
        private readonly LazyService<ClaimsPrincipal> _claimsPrincipalFactory;

        public GeexResultSerializerWithCustomStatusCodes(LazyService<ClaimsPrincipal> claimsPrincipalFactory)
        {
            _claimsPrincipalFactory = claimsPrincipalFactory;
        }

        /// <inheritdoc />
        protected override HttpStatusCode OnDetermineStatusCode(IQueryResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
        {
            var baseStatusCode = base.OnDetermineStatusCode(result, format, proposedStatusCode);

            if (result.Errors?.Count > 0)
            {
                if (result.Errors.Any(e => e.Code == ErrorCodes.Authentication.NotAuthorized || e.Code == ErrorCodes.Authentication.NotAuthenticated))
                {
                    var userId = _claimsPrincipalFactory.Value?.FindUserId();
                    return userId.IsNullOrEmpty() ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
                }

                if (result.Errors.Any(e => e.Exception is BusinessException business && business.ExceptionCode == GeexExceptionType.OnPurpose))
                {
                    return HttpStatusCode.InternalServerError;
                }

                return HttpStatusCode.BadRequest;

            }

            return baseStatusCode;
        }

    }
}
