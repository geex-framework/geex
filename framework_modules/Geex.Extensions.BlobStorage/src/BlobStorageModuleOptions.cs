namespace Geex.Extensions.BlobStorage
{
    public class BlobStorageModuleOptions : GeexModuleOption<BlobStorageModule>
    {
        /// <summary>
        /// File download path, default is "/download",
        /// </summary>
        public string FileDownloadPath { get; set; } = "/download";
        /// <summary>
        /// Local system storage path, default is "./App_Data/BlobStorageFiles"(relative to entry execution)
        /// </summary>
        public string FileSystemStoragePath { get; set; } = $"{GeexConstants.AppDataPath}/BlobStorageFiles";
    }
}
