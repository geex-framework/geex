namespace Geex.Extensions.BlobStorage
{
    public class BlobStorageType : Enumeration<BlobStorageType>
    {
        public static BlobStorageType Auto { get; } = FromValue(nameof(Auto));
        public static BlobStorageType Cache { get; } = FromValue(nameof(Cache));
        public static BlobStorageType Db { get; } = FromValue(nameof(Db));
        public static BlobStorageType FileSystem { get; } = FromValue(nameof(FileSystem));
    }
}
