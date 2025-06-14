using System.Threading.Tasks;

using MediatX;

namespace MediatX
{
    public interface IRequest : MediatR.IRequest { }
    public interface IRequest<out T> : MediatR.IRequest<T> { }
    public interface IEvent : MediatR.INotification { }
    public interface IDistributedEvent : IEvent { }
    public interface IRequestHandler<in TRequest, TResponse> : MediatR.IRequestHandler<TRequest, TResponse> where TRequest : MediatR.IRequest<TResponse> { }
    public interface IRequestHandler<in TRequest> : MediatR.IRequestHandler<TRequest> where TRequest : MediatR.IRequest { }
    public interface IEventHandler<in TEvent> : MediatR.INotificationHandler<TEvent> where TEvent : MediatR.INotification { }

    public interface IMediator : MediatR.IMediator
    {
        Task SendDistributedEvent<TRequest>(TRequest request) where TRequest : IDistributedEvent;
    }
}
