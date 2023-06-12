using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstractions;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RestSharp;
using Volo.Abp.Modularity;

namespace Geex.Bms.ConnectedServices
{
    public class BmsConnectedServiceModule : GeexModule<BmsConnectedServiceModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var moduleOptions = Configuration.GetModuleOptions<BmsConnectedServiceModuleOptions>();
            context.Services.AddTransient<LoggedRestClient>(x => new LoggedRestClient(x.GetService<ILogger<LoggedRestClient>>()));
            context.Services.AddTransient<RestClient, LoggedRestClient>();
            context.Services.AddSerializer<StrawberryShake.Serialization.JsonSerializer>();
            context.Services.AddMemoryCache();
            //context.Services.AddGeexApi().ConfigureHttpClient(async (sp, client) =>
            //{
            //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SuperAdmin", "83eebac535d14f791f6ee4dbefe689dc");
            //    client.BaseAddress = new Uri(Configuration["ConnectedServices:GeexApi:Endpoint"]);
            //});
            base.ConfigureServices(context);
        }
    }
}
