using Geex.Common.Captcha.Domain;
using MediatR;

namespace Geex.Common.Captcha.Requests
{
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
}