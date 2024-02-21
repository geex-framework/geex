using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace MediatX
{
  public interface IExternalMessageDispatcher
  {
    Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification;
  }
}
