using cqrs.models.Commands;
using cqrs.models.Dto;
using MediatR;

namespace receiver.handlers
{
  public class CommandHandler :
    IRequestHandler<MediatRRequest1, bool>,
    IRequestHandler<MediatRRequest2, ResponseDTO>,
    IRequestHandler<MediatRRequest3, IEnumerable<ResponseDTO>>,
    IRequestHandler<MediatRRequest4, long>,
    IRequestHandler<MediatRRequest5, int>,
    IRequestHandler<MediatRRequestWithHandlerException, bool>,
    INotificationHandler<MediatorNotification1>
  {
    public Task<ResponseDTO> Handle(MediatRRequest2 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(new ResponseDTO());
    }

    public Task<bool> Handle(MediatRRequest1 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(true);
    }

    public Task<bool> Handle(MediatRRequestWithHandlerException request, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public Task<IEnumerable<ResponseDTO>> Handle(MediatRRequest3 request, CancellationToken cancellationToken)
    {
      return Task.FromResult(new[] {new ResponseDTO()}.AsEnumerable());
    }

    public Task Handle(MediatorNotification1 notification, CancellationToken cancellationToken)
    {
      Console.WriteLine($"Notification received at : ${DateTime.Now.ToString("HH:mm:ss")}");
      return Task.CompletedTask;
    }

    public async Task<long> Handle(MediatRRequest4 request, CancellationToken cancellationToken)
    {
      return 9;
    }

    public async Task<int> Handle(MediatRRequest5 request, CancellationToken cancellationToken)
    {
      return 5;
    }
  }
}