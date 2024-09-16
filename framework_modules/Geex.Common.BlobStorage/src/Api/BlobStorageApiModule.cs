using System;
using System.IO;
using System.Security.Cryptography;

using Geex.Common.Abstractions;
using Volo.Abp.Modularity;

namespace Geex.Common.BlobStorage.Api
{
    public class BlobStorageApiModule : GeexModule<BlobStorageApiModule, BlobStorageModuleOptions>
    {
        /// <inheritdoc />
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var fileSystemStoragePath = this.ModuleOptions.FileSystemStoragePath;
            Directory.CreateDirectory(fileSystemStoragePath);
            base.ConfigureServices(context);
        }
    }
}
