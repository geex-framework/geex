using Geex.Common.Abstractions;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Core.Aggregates.Users;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.Identity.Api
{
    public class IdentityApiModule : GeexModule<IdentityApiModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IUserCreationValidator, UserCreationValidator>();
            base.ConfigureServices(context);
        }
    }
}
