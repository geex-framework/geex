using Geex.Common.Abstractions;

namespace Geex.Common.BlobStorage.Api
{
    public class BlobStorageModuleOptions : GeexModuleOption<BlobStorageApiModule>
    {
        public string FileDownloadPath { get; set; } = "/download";
    }
}
