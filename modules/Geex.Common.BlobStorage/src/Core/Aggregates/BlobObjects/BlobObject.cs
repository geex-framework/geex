using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects;
using Geex.Common.Abstractions;
using Geex.Common.BlobStorage.Api;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;
namespace Geex.Common.BlobStorage.Core.Aggregates.BlobObjects
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public class BlobObject : Abstraction.Storage.Entity<BlobObject>, IBlobObject
    {
        public BlobObject(string fileName, string md5, BlobStorageType storageType, string mimeType, long fileSize)
        {
            this.FileName = fileName;
            this.Md5 = md5;
            this.MimeType = mimeType;
            this.FileSize = fileSize;
            this.StorageType = storageType;
        }
        [Obsolete("for internal use only.", false)]
        public BlobObject()
        {

        }

        public string FileName { get; set; }
        public string Md5 { get; set; }
        public string Url =>
            $"{DbContext.ServiceProvider.GetService<GeexCoreModuleOptions>().Host.Trim('/')}/{DbContext.ServiceProvider.GetService<BlobStorageModuleOptions>().FileDownloadPath.Trim('/')}?fileId={this.Id}&storageType={this.StorageType}";
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public BlobStorageType StorageType { get; set; }
        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }
    }
}
