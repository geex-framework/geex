using Geex.Common.Abstractions;

namespace Geex.Common.BlobStorage.Api
{
    public class BlobStorageModuleOptions : GeexModuleOption<BlobStorageApiModule>
    {
        /// <summary>
        /// File download path, default is "/download",
        /// </summary>
        public string FileDownloadPath { get; set; } = "/download";
        /// <summary>
        /// Local system storage path, default is "./_BlobStorageFiles"(relative to entry execution)
        /// </summary>
        public string FileSystemStoragePath { get; set; } = "./App_Data/BlobStorageFiles";
    }
}
