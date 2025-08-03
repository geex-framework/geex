using System.IO;
using Geex.Extensions.BlobStorage.Extensions;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnPreApplicationInitialization(context);
        }

        /// <inheritdoc />
        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseEndpoints(endpoints => endpoints.UseFileDownload());
            base.OnPostApplicationInitialization(context);
        }
    }
}
