using System.Threading.Tasks;
using Geex.Extensions.Captcha.Abstractions.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Captcha.Gql;

public sealed class CaptchaMutation : MutationExtension<CaptchaMutation>
{
    private readonly IUnitOfWork _uow;

    public CaptchaMutation(IUnitOfWork uow)
    {
        _uow = uow;
    }

    protected override void Configure(IObjectTypeDescriptor<CaptchaMutation> descriptor)
    {
        base.Configure(descriptor);
    }

    public async Task<Core.Captcha> GenerateCaptcha(SendCaptchaRequest request) => await _uow.Request(request);

    public async Task<bool> ValidateCaptcha(ValidateCaptchaRequest request) => await _uow.Request(request);
}
