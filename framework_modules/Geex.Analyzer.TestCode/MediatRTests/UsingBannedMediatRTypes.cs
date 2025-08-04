using System.Threading;
using System.Threading.Tasks;

namespace Geex.Analyzer.TestCode.MediatRTests
{
    // 测试用例：使用被禁止的 MediatR 类型
    public class BadMediatRUsageTest
    {
        // 应该报告 GEEX002
        private readonly MediatR.IMediator mediator;

        public BadMediatRUsageTest(MediatR.IMediator mediator)
        {
            this.mediator = mediator;
        }

        // 应该报告 GEEX002
        public class BadRequest : MediatR.IRequest
        {
        }

        // 应该报告 GEEX002
        public class BadRequestHandler : MediatR.IRequestHandler<BadRequest>
        {
            public Task Handle(BadRequest request, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        // 应该报告 GEEX002
        public class BadNotification : MediatR.INotification
        {
        }

        // 应该报告 GEEX002
        public class BadNotificationHandler : MediatR.INotificationHandler<BadNotification>
        {
            public Task Handle(BadNotification notification, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
