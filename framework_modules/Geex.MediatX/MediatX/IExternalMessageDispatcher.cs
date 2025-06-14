using System.Threading;
using System.Threading.Tasks;

namespace MediatX
{
  public interface IExternalMessageDispatcher
  {
    Task Notify<TRequest>(string routingKey, TRequest request, CancellationToken cancellationToken = default) where TRequest : IEvent;
  }
}
