using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Captcha.Abstractions;
using Geex.Extensions.Captcha.Abstractions.Requests;
using Geex.Extensions.Captcha.Core;
using Geex.Extensions.Messaging.Requests;
using MediatX;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Captcha.Core.Handlers;

public class CaptchaHandler :
    IRequestHandler<SendCaptchaRequest, Captcha>,
    IRequestHandler<ValidateCaptchaRequest, bool>
{
    private readonly IRedisDatabase _cache;
    private readonly IUnitOfWork _uow;

    public CaptchaHandler(IRedisDatabase cache, IUnitOfWork uow)
    {
        _cache = cache;
        _uow = uow;
    }

    public async Task<Captcha> Handle(SendCaptchaRequest request, CancellationToken cancellationToken)
    {
        if (request.CaptchaProvider == CaptchaProvider.Sms)
        {
            var captcha = new SmsCaptcha();
            await _cache.SetNamedAsync(captcha, token: cancellationToken);
            await _uow.Request(new SendSmsRequest(request.SmsCaptchaPhoneNumber!, [captcha.Code]), cancellationToken);
            return captcha;
        }

        if (request.CaptchaProvider == CaptchaProvider.Image)
        {
            var captcha = new ImageCaptcha();
            await _cache.SetNamedAsync(captcha, token: cancellationToken);
            return captcha;
        }

        throw new System.ArgumentOutOfRangeException(nameof(request.CaptchaProvider));
    }

    public async Task<bool> Handle(ValidateCaptchaRequest request, CancellationToken cancellationToken)
    {
        if (request.CaptchaProvider == CaptchaProvider.Sms)
        {
            var captcha = await _cache.GetAsync<SmsCaptcha>(request.CaptchaKey);
            return captcha?.Code == request.CaptchaCode;
        }

        if (request.CaptchaProvider == CaptchaProvider.Image)
        {
            var captcha = await _cache.GetAsync<ImageCaptcha>(request.CaptchaKey);
            return captcha?.Code == request.CaptchaCode;
        }

        throw new System.ArgumentOutOfRangeException(nameof(request.CaptchaProvider));
    }
}
