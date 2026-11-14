using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Requests;
using MediatX;
using MongoDB.Entities.Utilities;

namespace Geex.Extensions.BlobStorage.Core.Handlers
{
    public class BlobObjectHandler :
        ICommonHandler<IBlobObject, BlobObject>,
        IRequestHandler<CreateBlobObjectRequest, IBlobObject>,
        IRequestHandler<DeleteBlobObjectRequest>
    {
        public BlobObjectHandler(IUnitOfWork uow)
        {
            Uow = uow;
        }

        public IUnitOfWork Uow { get; }

        public virtual async Task<IBlobObject> Handle(CreateBlobObjectRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Uow.Create(request);
        }

        public virtual Task<IQueryable<IBlobObject>> Handle(QueryRequest<IBlobObject> request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var blobs = Uow.Query<BlobObject>().Where(x => x.UploadPending != true);
            if (request.Filter != null)
            {
                blobs = blobs.Where((Expression<Func<BlobObject, bool>>)request.Filter.CastParamType<BlobObject>());
            }
            return Task.FromResult<IQueryable<IBlobObject>>(blobs);
        }

        public virtual async Task Handle(DeleteBlobObjectRequest request, CancellationToken cancellationToken)
        {
            var blobObjects = Uow.Query<BlobObject>().Where(x => request.Ids.Contains(x.Id)).ToList();

            foreach (var blobObject in blobObjects)
            {
                await blobObject.DeleteAsync(cancellationToken);
            }
        }
    }
}
