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
    public class GeexHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        private readonly ICurrentUser _currentUser;

        public GeexHttpResponseFormatter(ICurrentUser currentUser)
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

                if (result.Errors.All(x => x.Code == ErrorCodes.Execution.NonNullViolation || x.Code == ErrorCodes.Execution.CannotResolveAbstractType))
                {
                    return HttpStatusCode.OK;
                }

                if (result.Errors.Any(e => e.Exception != default))
                {
                    return HttpStatusCode.InternalServerError;
                }

                return baseStatusCode;

            }

            return baseStatusCode;
        }
    }
}
