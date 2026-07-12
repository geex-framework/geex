using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Messaging.Requests;
using MediatX;
using Volo.Abp.DependencyInjection;

namespace Geex.Extensions.Messaging.Core.Handlers;

public class SmsHandler : IRequestHandler<SendSmsRequest, bool>, ITransientDependency
{
    private readonly ISmsSender _smsSender;

    public SmsHandler(ISmsSender smsSender)
    {
        _smsSender = smsSender;
    }

    public async Task<bool> Handle(SendSmsRequest request, CancellationToken cancellationToken)
    {
        await _smsSender.SendAsync(request.PhoneNumber, request.TemplateParams, cancellationToken);
        return true;
    }
}
