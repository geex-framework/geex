using System.Threading;
using System.Threading.Tasks;
using MediatR; // 应该报告 GEEX001

namespace Geex.Analyzer.TestCode.MediatRTests
{
    // 复杂的 MediatR 使用场景 - 应该报告多个诊断
    public class ComplexMediatRUsageTest
    {
        private readonly IMediator _mediator; // GEEX002

        public ComplexMediatRUsageTest(IMediator mediator) // GEEX002
        {
            _mediator = mediator;
        }

        public async Task ExecuteAsync()
        {
            var request = new ComplexRequest();
            await _mediator.Send(request);
            
            var notification = new ComplexNotification();
            await _mediator.Publish(notification);
        }
    }

    public class ComplexRequest : IRequest<ComplexResponse> // GEEX002
    {
        public string Data { get; set; }
    }

    public class ComplexResponse
    {
        public string Result { get; set; }
    }

    public class ComplexRequestHandler : IRequestHandler<ComplexRequest, ComplexResponse> // GEEX002
    {
        public Task<ComplexResponse> Handle(ComplexRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ComplexResponse { Result = request.Data });
        }
    }

    public class ComplexNotification : INotification // GEEX002
    {
        public string Message { get; set; }
    }

    public class ComplexNotificationHandler : INotificationHandler<ComplexNotification> // GEEX002
    {
        public Task Handle(ComplexNotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
