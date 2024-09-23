using System.Threading.Tasks;

using MediatR;

namespace MediatX
{
    public interface IRequest : MediatR.IRequest { }
    public interface IRequest<out T> : MediatR.IRequest<T> { }
    public interface INotification : MediatR.INotification { }
    public interface IRemoteNotification : INotification { }
    public interface IMediatX
    {
        Task SendRemoteNotification<TRequest>(TRequest request) where TRequest : IRemoteNotification;
    }
}
