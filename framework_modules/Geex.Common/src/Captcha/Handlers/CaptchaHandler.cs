using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Captcha.Domain;
using Geex.Common.Requests.Captcha;
using Geex.Extensions.Messaging.Requests;
using MediatX;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Captcha.Handlers
{
    public class CaptchaHandler : IRequestHandler<SendCaptchaRequest, Domain.Captcha>, IRequestHandler<ValidateCaptchaRequest, bool>
    {
        private IRedisDatabase _cache;
        private IUnitOfWork _uow;

        public CaptchaHandler(IRedisDatabase cache, IUnitOfWork uow)
        {
            _cache = cache;
            _uow = uow;
        }

        public async Task<Domain.Captcha> Handle(SendCaptchaRequest request, CancellationToken cancellationToken)
        {
            if (request.CaptchaProvider == CaptchaProvider.Sms)
            {
                var captcha = new SmsCaptcha();
                await _cache.SetNamedAsync(captcha, token: cancellationToken);
                await _uow.Request(new SendSmsRequest(request.SmsCaptchaPhoneNumber, [captcha.Code]), cancellationToken);
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
                if (captcha?.Code != request.CaptchaCode)
                {
                    return false;
                }
                return true;
            }

            if (request.CaptchaProvider == CaptchaProvider.Image)
            {
                var captcha = await _cache.GetAsync<ImageCaptcha>(request.CaptchaKey);
                if (captcha?.Code != request.CaptchaCode)
                {
                    return false;
                }
                return true;
            }
            throw new System.ArgumentOutOfRangeException(nameof(request.CaptchaProvider));
        }
    }
}
