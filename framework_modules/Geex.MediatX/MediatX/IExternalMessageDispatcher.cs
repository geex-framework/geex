using System.Threading;
using System.Threading.Tasks;

namespace MediatX
{
  public interface IExternalMessageDispatcher
  {
    Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IEvent;
  }
}
