using System;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Captcha.Commands;
using Geex.Common.Captcha.Domain;
using Geex.Common.Captcha.GqlSchemas.Inputs;
using HotChocolate;
using MediatR;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Captcha
{
    public class CaptchaMutation : MutationExtension<CaptchaMutation>
    {
        private readonly IMediator _mediator;

        public CaptchaMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        public async Task<Domain.Captcha> GenerateCaptcha(
            [Service] IRedisDatabase cache,
            SendCaptchaInput input)
        {
            if (input.CaptchaProvider == CaptchaProvider.Sms)
            {
                IRedisCacheClient a;
                var captcha = new SmsCaptcha();
                await cache.SetNamedAsync(captcha);
                await this._mediator.Send(new SendSmsCaptchaRequest(input.SmsCaptchaPhoneNumber, captcha));
                return captcha;
            }

            if (input.CaptchaProvider == CaptchaProvider.Image)
            {
                var captcha = new ImageCaptcha();
                await cache.SetNamedAsync(captcha);
                return captcha;
            }
            throw new ArgumentOutOfRangeException("input.CaptchaProvider");
        }

        public async Task<bool> ValidateCaptcha(
            [Service] IRedisDatabase cache,
            ValidateCaptchaInput input)
        {
            if (input.CaptchaProvider == CaptchaProvider.Sms)
            {
                var captcha = await cache.GetAsync<SmsCaptcha>(input.CaptchaKey);
                if (captcha.Code != input.CaptchaCode)
                {
                    return false;
                }
                return true;
            }

            if (input.CaptchaProvider == CaptchaProvider.Image)
            {
                var captcha = await cache.GetAsync<ImageCaptcha>(input.CaptchaKey);
                if (captcha.Code != input.CaptchaCode)
                {
                    return false;
                }
                return true;
            }
            throw new ArgumentOutOfRangeException("input.CaptchaProvider");
        }
    }

    public class ValidateCaptchaInput
    {
        public string CaptchaKey { get; set; }
        public CaptchaProvider CaptchaProvider { get; set; }
        public string CaptchaCode { get; set; }
    }
}
