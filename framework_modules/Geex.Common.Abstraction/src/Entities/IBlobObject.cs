using System.IO;
using System.Threading.Tasks;

using MongoDB.Entities;

namespace Geex.Common.Abstraction.Entities
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public interface IBlobObject : IEntityBase
    {
        public string? FileName { get; set; }
        public string? Md5 { get; set; }
        public long FileSize { get; }
        public string? MimeType { get; }
        public string? Url { get; }
        public BlobStorageType? StorageType { get; }
        public Task<Stream> GetFileContent();
    }
}
