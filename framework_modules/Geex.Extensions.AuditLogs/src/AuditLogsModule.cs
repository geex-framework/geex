using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Abstractions;
using HotChocolate;
using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Driver;
using MongoDB.Entities;

using OpenIddict.Abstractions;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.AuditLogs
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class AuditLogsModule : GeexModule<AuditLogsModule, AuditLogsModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }

        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}
