using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Core.Aggregates.BlobObjects
{
    public class DbFile : FileEntity
    {
        public DbFile(string md5)
        {
            Md5 = md5;
        }

        public string Md5 { get; set; }
    }
}
