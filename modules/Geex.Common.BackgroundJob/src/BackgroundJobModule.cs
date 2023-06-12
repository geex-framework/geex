using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyCronJob.Abstractions;
using EasyCronJob.Core;
using Geex.Common.Abstraction;
using Geex.Common.Abstractions;

using HotChocolate.Execution.Configuration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.BackgroundJob
{
    [DependsOn(typeof(GeexCoreModule))]
    public class BackgroundJobModule : GeexModule<BackgroundJobModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            //var moduleOptions = Configuration.GetModuleOptions<BackgroundJobModuleOptions>();
            base.PostConfigureServices(context);
        }

        /// <inheritdoc />
        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();

            return base.OnPreApplicationInitializationAsync(context);
        }

        /// <inheritdoc />
        public override Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var moduleOptions = Configuration.GetModuleOptions<BackgroundJobModuleOptions>();
            return base.OnPostApplicationInitializationAsync(context);
        }
    }
}
