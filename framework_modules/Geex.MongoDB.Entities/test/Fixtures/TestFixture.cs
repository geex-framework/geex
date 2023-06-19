using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MongoDB.Entities.Tests.Fixtures
{
    internal class TestFixture
    {
        public static IServiceCollection ServiceCollection { get; } = new ServiceCollection().AddLogging(x => x.AddDebug());

        public static IServiceProvider ServiceProvider => new DefaultServiceProviderFactory(new ServiceProviderOptions()).CreateServiceProvider(ServiceCollection);
    }
}
