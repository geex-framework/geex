using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Requests.Captcha;
using HotChocolate;
using MediatR;
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
            SendCaptchaRequest request)
        {
            return await _mediator.Send(request);
        }

        public async Task<bool> ValidateCaptcha(
            [Service] IRedisDatabase cache,
            ValidateCaptchaRequest request)
        {
            return await _mediator.Send(request);
        }
    }
}
