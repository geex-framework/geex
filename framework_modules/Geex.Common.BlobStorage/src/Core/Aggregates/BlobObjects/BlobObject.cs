using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api;
using Geex.Common.BlobStorage.Api.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;
using MediatR;

namespace Geex.Common.BlobStorage.Core.Aggregates.BlobObjects
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public class BlobObject : Abstraction.Storage.Entity<BlobObject>, IBlobObject
    {
        public BlobObject(string fileName, string md5, BlobStorageType storageType, string mimeType, long fileSize,IUnitOfWork uow = default)
        {
            this.FileName = fileName;
            this.Md5 = md5;
            this.MimeType = mimeType;
            this.FileSize = fileSize;
            this.StorageType = storageType;
        }
        [Obsolete("for internal use only.", false)]
        internal BlobObject()
        {

        }

        public string? FileName { get; set; }
        public string? Md5 { get; set; }
        public string? Url =>
            $"{ServiceProvider.GetService<GeexCoreModuleOptions>().Host.Trim('/')}/{ServiceProvider.GetService<BlobStorageModuleOptions>().FileDownloadPath.Trim('/')}?fileId={this.Id}";
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public BlobStorageType StorageType { get; set; }
        public DateTimeOffset? ExpireAt { get; set; }

        public async Task<Stream> GetFileContent() => (await ServiceProvider.GetService<IUnitOfWork>()
            .Request(new DownloadFileRequest(this.Id))).dataStream;
        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }

        public class BlobObjectBsonConfig : BsonConfig<BlobObject>
        {
            protected override void Map(BsonClassMap<BlobObject> map, BsonIndexConfig<BlobObject> indexConfig)
            {
                map.Inherit<IBlobObject>();
                map.AutoMap();
                indexConfig.MapEntityDefaultIndex();
                indexConfig.MapIndex(x => x.Hashed(y => y.Md5), options =>
                {
                    options.Background = true;
                });
                indexConfig.MapIndex(x => x.Ascending(y => y.ExpireAt), options =>
                {
                    options.ExpireAfter = TimeSpan.FromSeconds(0);
                });
            }
        }


        public class BlobObjectGqlConfig : GqlConfig.Object<BlobObject>
        {

            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<BlobObject> descriptor)
            {
                descriptor.Implements<InterfaceType<IBlobObject>>();
                descriptor.BindFieldsImplicitly();
                descriptor.ConfigEntity();
            }
        }
    }
}
