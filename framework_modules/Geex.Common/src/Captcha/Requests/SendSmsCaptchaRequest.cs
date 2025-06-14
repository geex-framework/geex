using Geex.Common.Captcha.Domain;
using MediatX;

namespace Geex.Common.Requests.Captcha
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