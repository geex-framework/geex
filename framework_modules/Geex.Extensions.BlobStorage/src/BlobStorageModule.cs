using System.IO;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.BlobStorage.Extensions;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.BlobStorage
{
    [DependsOn(typeof(GeexCoreModule))]
    public class BlobStorageModule : GeexModule<BlobStorageModule, BlobStorageModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SchemaBuilder.AddType<UploadType>();
            context.Services.AddMemoryCache();


             var fileSystemStoragePath = this.ModuleOptions.FileSystemStoragePath;
            Directory.CreateDirectory(fileSystemStoragePath);

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
