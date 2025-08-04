using MediatX;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Analyzer.TestCode.MediatRTests
{
    // 测试正确使用 MediatX 命名空间和类型 - 不应该报告任何诊断
    public class UsingMediatXTypesTest
    {
        private readonly IMediator _mediator;

        public UsingMediatXTypesTest(IMediator mediator)
        {
            _mediator = mediator;
        }
    }

    // 正确的 MediatX 接口实现
    public class CorrectRequest : IRequest<string>
    {
        public string Name { get; set; }
    }

    public class CorrectRequestHandler : IRequestHandler<CorrectRequest, string>
    {
        public Task<string> Handle(CorrectRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Hello {request.Name}");
        }
    }

    public class CorrectEvent : IEvent
    {
        public string Message { get; set; }
    }

    public class CorrectEventHandler : IEventHandler<CorrectEvent>
    {
        public Task Handle(CorrectEvent @event, CancellationToken cancellationToken)
        {
            // Handle event
            return Task.CompletedTask;
        }
    }
}

// 提供MediatX命名空间以便测试
namespace MediatX
{
    public interface IMediator { }
    public interface IRequest<T> { }
    public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
    public interface IEvent { }
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        Task Handle(TEvent @event, CancellationToken cancellationToken);
    }
}
