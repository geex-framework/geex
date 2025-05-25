using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Common.Requests.Captcha;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Types;

using MediatR;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Captcha
{
    public sealed class CaptchaMutation : MutationExtension<CaptchaMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<CaptchaMutation> descriptor)
        {
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public CaptchaMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<Domain.Captcha> GenerateCaptcha(SendCaptchaRequest request) => await _uow.Request(request);

        public async Task<bool> ValidateCaptcha(ValidateCaptchaRequest request) => await _uow.Request(request);
    }
}
