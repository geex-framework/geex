using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Tests
{
    public class TestsBase(WebApplicationFactory<Program> factory) : IAsyncLifetime
    {
        public WebApplicationFactory<Program> Factory { get; } = factory;

        /// <inheritdoc />
        public virtual async Task InitializeAsync()
        {
            this.Scope = factory.Services.CreateScope();
            this.Service = Scope.ServiceProvider;
        }

        public IServiceProvider Service { get; set; }
        public string GqlEndpoint { get; set; } = "/graphql";

        public IServiceScope Scope { get; set; }

        /// <inheritdoc />
        public virtual async Task DisposeAsync()
        {
            this.Scope.Dispose();
        }
    }
}
