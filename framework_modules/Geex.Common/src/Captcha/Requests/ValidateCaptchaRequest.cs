using Geex.Common.Captcha.Domain;

using MediatR;

namespace Geex.Common.Requests.Captcha;

public record ValidateCaptchaRequest : IRequest<bool>
{
    public string CaptchaKey { get; set; }
    public CaptchaProvider CaptchaProvider { get; set; }
    public string CaptchaCode { get; set; }
}