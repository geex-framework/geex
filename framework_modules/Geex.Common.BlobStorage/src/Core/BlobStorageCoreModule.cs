using System.IO;
using System.Threading.Tasks;

using Geex.Common.Abstractions;
using Geex.Common.BlobStorage.Api;
using Geex.Common.BlobStorage.Api.Abstractions;
using Geex.Common.BlobStorage.Core.Services;

using HotChocolate.Types;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.BlobStorage.Core
{
    [DependsOn(typeof(BlobStorageApiModule))]
    public class BlobStorageCoreModule : GeexModule<BlobStorageCoreModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SchemaBuilder.AddType<UploadType>();
            context.Services.AddMemoryCache();
            
            // Register BlobStorageService
            context.Services.AddScoped<IBlobService, BlobService>();
            
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            return base.OnPreApplicationInitializationAsync(context);
        }

        /// <inheritdoc />
        public override Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseEndpoints(endpoints => endpoints.UseFileDownload());
            return base.OnPostApplicationInitializationAsync(context);
        }
    }
}
