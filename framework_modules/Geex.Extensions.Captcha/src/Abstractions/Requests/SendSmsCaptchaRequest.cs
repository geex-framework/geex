using Geex.Extensions.Captcha.Core.Entities;
using MediatX;

namespace Geex.Extensions.Captcha.Abstractions.Requests;

public record SendSmsCaptchaRequest : IRequest
{
    public string PhoneNumber { get; }
    public SmsCaptcha Captcha { get; }

    public SendSmsCaptchaRequest(string phoneNumber, SmsCaptcha captcha)
    {
        PhoneNumber = phoneNumber;
        Captcha = captcha;
    }
}
