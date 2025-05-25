using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using HotChocolate;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Gql
{
    public class GeexHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        private readonly IServiceProvider _serviceProvider;

        public GeexHttpResponseFormatter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        protected override HttpStatusCode OnDetermineStatusCode(IQueryResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
        {
            var baseStatusCode = base.OnDetermineStatusCode(result, format, proposedStatusCode);

            if (result.Errors?.Count > 0)
            {
                if (result.Errors.Any(e => e.Code == ErrorCodes.Authentication.NotAuthorized || e.Code == ErrorCodes.Authentication.NotAuthenticated))
                {
                    var authenticated = _serviceProvider.GetService<ClaimsPrincipal>().Identity.IsAuthenticated;
                    return authenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
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
