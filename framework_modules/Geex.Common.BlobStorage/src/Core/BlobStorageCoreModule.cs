using System.Threading.Tasks;

using Geex.Common.Abstractions;
using Geex.Common.BlobStorage.Api;

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
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseEndpoints(endpoints => endpoints.UseFileDownload());
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}
