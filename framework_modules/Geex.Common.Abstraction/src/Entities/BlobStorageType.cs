using Geex.Common.Abstractions;

namespace Geex.Common.Abstraction.Entities
{
    public class BlobStorageType : Enumeration<BlobStorageType>
    {
        public BlobStorageType(string name, string value) : base(name, value)
        {
        }

        public static BlobStorageType AliyunOss { get; } = new BlobStorageType(nameof(AliyunOss), nameof(AliyunOss));
        public static BlobStorageType Cache { get; } = new BlobStorageType(nameof(Cache), nameof(Cache));
        public static BlobStorageType Db { get; } = new BlobStorageType(nameof(Db), nameof(Db));
        public static BlobStorageType FileSystem { get; } = new BlobStorageType(nameof(FileSystem), nameof(FileSystem));

    }
}
