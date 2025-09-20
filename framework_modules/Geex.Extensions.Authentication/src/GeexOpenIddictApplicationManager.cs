using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.MongoDb.Models;
using OpenIddict.Server;

namespace Geex.Extensions.Authentication
{
    public class GeexOpenIddictApplicationManager : OpenIddictApplicationManager<OpenIddictMongoDbApplication>
    {
        public GeexOpenIddictApplicationManager(
            IOpenIddictApplicationCache<OpenIddictMongoDbApplication> cache,
            ILogger<OpenIddictApplicationManager<OpenIddictMongoDbApplication>> logger,
            IOptionsMonitor<OpenIddictCoreOptions> options,
            IOpenIddictApplicationStoreResolver resolver)
            : base(cache, logger, options, resolver) { }

        /// <inheritdoc />
        public override ValueTask<bool> ValidateRedirectUriAsync(OpenIddictMongoDbApplication application, string uri,
            CancellationToken cancellationToken = new CancellationToken())
        {
            // Get the list of valid redirect URIs from your custom application model.
            var validRedirectUris = application.RedirectUris;

            foreach (var validRedirectUri in validRedirectUris)
            {
                if (uri.StartsWith(validRedirectUri, StringComparison.OrdinalIgnoreCase))
                {
                    return ValueTask.FromResult(true);
                }
            }
            Logger.LogWarning("Invalid redirect URI: {Uri}", uri);
            return ValueTask.FromResult(false);
        }

        /// <inheritdoc />
        public override ValueTask<bool> ValidatePostLogoutRedirectUriAsync(OpenIddictMongoDbApplication application, string uri,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var validPostLogoutRedirectUris = application.PostLogoutRedirectUris;

            foreach (var validPostLogoutRedirectUri in validPostLogoutRedirectUris)
            {
                if (uri.StartsWith(validPostLogoutRedirectUri, StringComparison.OrdinalIgnoreCase))
                {
                    return ValueTask.FromResult(true);
                }
            }

            Logger.LogWarning("Invalid post logout redirect URI: {Uri}", uri);
            return ValueTask.FromResult(false);
        }
    }
}
