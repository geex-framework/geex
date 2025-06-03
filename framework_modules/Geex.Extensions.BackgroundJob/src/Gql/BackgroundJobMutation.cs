using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.BackgroundJob.Gql
{
    public sealed class BackgroundJobMutation : MutationExtension<BackgroundJobMutation>
    {
        private readonly IUnitOfWork _uow;

        public BackgroundJobMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }
    }
}
