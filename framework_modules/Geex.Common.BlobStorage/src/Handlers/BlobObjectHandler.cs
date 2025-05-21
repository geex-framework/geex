using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api;
using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using Geex.Common.Requests.BlobStorage;
using MediatR;
using Geex.Common.BlobStorage.Requests;

namespace Geex.Common.BlobStorage.Core.Handlers
{
    public class BlobObjectHandler :
        ICommonHandler<IBlobObject, BlobObject>,
        IRequestHandler<CreateBlobObjectRequest, IBlobObject>,
        IRequestHandler<DeleteBlobObjectRequest>,
        IRequestHandler<DownloadFileRequest, (IBlobObject blob, Stream dataStream)>
    {
        public BlobObjectHandler(IUnitOfWork uow)
        {
            Uow = uow;
        }

        public IUnitOfWork Uow { get; }

        public virtual async Task<IBlobObject> Handle(CreateBlobObjectRequest request, CancellationToken cancellationToken)
        {
            return Uow.Create(request);
        }

        public virtual async Task Handle(DeleteBlobObjectRequest request, CancellationToken cancellationToken)
        {
            var blobObjects = await Task.FromResult(Uow.Query<BlobObject>().Where(x => request.Ids.Contains(x.Id)));

            foreach (var blobObject in blobObjects)
            {
                await blobObject.DeleteAsync(cancellationToken);
            }
        }

        public virtual async Task<(IBlobObject blob, Stream dataStream)> Handle(DownloadFileRequest request, CancellationToken cancellationToken)
        {
            var blob = await Task.FromResult(Uow.Query<BlobObject>().First(x => x.Id == request.BlobId));
            var dataStream = await blob.StreamFromStorage(cancellationToken);

            return (blob, dataStream);
        }
    }
}
