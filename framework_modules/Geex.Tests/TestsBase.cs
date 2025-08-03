using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Identity;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Tests
{
    public class TestsBase(WebApplicationFactory<Program> factory) : IAsyncLifetime
    {
        private WebApplicationFactory<Program> Factory { get; } = factory;

        /// <inheritdoc />
        public virtual async Task InitializeAsync()
        {
            this.RootService = Factory.Services;
            this.Scope = RootService.CreateScope();
            this.ScopedService = Scope.ServiceProvider;
        }

        public IServiceProvider RootService { get; set; }

        public IServiceProvider ScopedService { get; set; }
        public string GqlEndpoint { get; set; } = "/graphql";

        public HttpClient SuperAdminClient
        {
            get
            {
                _ = this.ScopedService.GetService<SuperAdminAuthHandler>();
                var token = SuperAdminAuthHandler.AdminTokenStr;
                var client = this.Factory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"SuperAdmin {token}");
                return client;
            }
        }

        public HttpClient AnonymousClient => Factory.CreateClient();
        public HttpClient UserClient(string userIdentifier)
        {
            var tokenHandler = this.ScopedService.GetService<GeexJwtSecurityTokenHandler>();
            var tokenGenerateOptions = this.ScopedService.GetService<UserTokenGenerateOptions>();
            var user = GeexCommonAbstractionEntitiesIUserExtensions.MatchUserIdentifier(this.ScopedService.GetService<IUnitOfWork>().Query<IUser>(), userIdentifier);
            var token = tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, tokenGenerateOptions));
            var client = this.Factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Local {token}");
            return client;
        }

        public IServiceScope Scope { get; set; }

        /// <inheritdoc />
        public virtual async Task DisposeAsync()
        {
            this.Scope.Dispose();
        }
    }
}
