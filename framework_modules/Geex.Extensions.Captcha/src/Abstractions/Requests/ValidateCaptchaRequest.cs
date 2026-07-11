using Geex.Extensions.Captcha.Core.Entities;
using MediatX;

namespace Geex.Extensions.Captcha.Abstractions.Requests;

public record ValidateCaptchaRequest : IRequest<bool>
{
    public string CaptchaKey { get; set; } = string.Empty;
    public CaptchaProvider CaptchaProvider { get; set; } = default!;
    public string CaptchaCode { get; set; } = string.Empty;
}
