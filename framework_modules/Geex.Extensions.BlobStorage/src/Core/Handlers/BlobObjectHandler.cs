using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Requests;
using MediatX;

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
    }
}
