using System;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Common.Captcha.Domain;
using Geex.Common.Requests.Captcha;
using MediatX;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Captcha.Handlers
{
    public class CaptchaHandler : IRequestHandler<SendSmsCaptchaRequest>, IRequestHandler<SendCaptchaRequest, Domain.Captcha>, IRequestHandler<ValidateCaptchaRequest, bool>
    {
        private IRedisDatabase _cache;
        private IUnitOfWork _uow;

        public CaptchaHandler(IRedisDatabase cache, IUnitOfWork uow)
        {
            _cache = cache;
            _uow = uow;
        }

        /// <inheritdoc />
        public Task Handle(SendSmsCaptchaRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        /// <inheritdoc />
        public async Task<Domain.Captcha> Handle(SendCaptchaRequest request, CancellationToken cancellationToken)
        {
            if (request.CaptchaProvider == CaptchaProvider.Sms)
            {
                var captcha = new SmsCaptcha();
                await _cache.SetNamedAsync(captcha, token: cancellationToken);
                await this._uow.Request(new SendSmsCaptchaRequest(request.SmsCaptchaPhoneNumber, captcha), cancellationToken);
                return captcha;
            }

            if (request.CaptchaProvider == CaptchaProvider.Image)
            {
                var captcha = new ImageCaptcha();
                await _cache.SetNamedAsync(captcha, token: cancellationToken);
                return captcha;
            }
            throw new ArgumentOutOfRangeException("input.CaptchaProvider");
        }

        /// <inheritdoc />
        public async Task<bool> Handle(ValidateCaptchaRequest request, CancellationToken cancellationToken)
        {
            if (request.CaptchaProvider == CaptchaProvider.Sms)
            {
                var captcha = await _cache.GetAsync<SmsCaptcha>(request.CaptchaKey);
                if (captcha.Code != request.CaptchaCode)
                {
                    return false;
                }
                return true;
            }

            if (request.CaptchaProvider == CaptchaProvider.Image)
            {
                var captcha = await _cache.GetAsync<ImageCaptcha>(request.CaptchaKey);
                if (captcha.Code != request.CaptchaCode)
                {
                    return false;
                }
                return true;
            }
            throw new ArgumentOutOfRangeException("input.CaptchaProvider");
        }
    }
}
