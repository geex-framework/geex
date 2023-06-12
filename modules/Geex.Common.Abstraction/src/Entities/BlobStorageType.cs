using Geex.Common.Abstractions;

namespace Geex.Common.Abstraction.Entities
{
    public class BlobStorageType : Enumeration<BlobStorageType>
    {
        public BlobStorageType(string name, string value) : base(name, value)
        {
        }

        public static BlobStorageType AliyunOss { get; } = new BlobStorageType(nameof(AliyunOss), nameof(AliyunOss));
        public static BlobStorageType RedisCache { get; } = new BlobStorageType(nameof(RedisCache), nameof(RedisCache));
        public static BlobStorageType Db { get; } = new BlobStorageType(nameof(Db), nameof(Db));

    }
}
