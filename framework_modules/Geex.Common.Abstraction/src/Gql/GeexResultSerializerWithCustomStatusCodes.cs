using System;
using System.Linq;
using System.Net;
using System.Security.Claims;

using Geex.Common.Abstraction.Authentication;
using Geex.Common.Abstractions;

using HotChocolate;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;

namespace Geex.Common.Abstraction.Gql
{
    public class GeexResultSerializerWithCustomStatusCodes : DefaultHttpResponseFormatter
    {
        private readonly ICurrentUser _currentUser;

        public GeexResultSerializerWithCustomStatusCodes(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        /// <inheritdoc />
        protected override HttpStatusCode OnDetermineStatusCode(IQueryResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
        {
            var baseStatusCode = base.OnDetermineStatusCode(result, format, proposedStatusCode);

            if (result.Errors?.Count > 0)
            {
                if (result.Errors.Any(e => e.Code == ErrorCodes.Authentication.NotAuthorized || e.Code == ErrorCodes.Authentication.NotAuthenticated))
                {
                    var userId = _currentUser.UserId;
                    return userId.IsNullOrEmpty() ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
                }

                if (result.Errors.Any(e => e.Exception is BusinessException business && business.ExceptionCode == GeexExceptionType.OnPurpose))
                {
                    return HttpStatusCode.InternalServerError;
                }

                if (result.Errors.All(x=>x.Exception == null))
                {
                    return baseStatusCode;
                }

                return HttpStatusCode.BadRequest;

            }

            return baseStatusCode;
        }
    }
}
