using Geex.Common.Captcha.Domain;
using Geex.Common.Gql.Types.Scalars;

using HotChocolate;

using MediatR;

namespace Geex.Common.Captcha.Requests
{
    public record SendCaptchaRequest : IRequest<Domain.Captcha>
    {
        public CaptchaProvider CaptchaProvider { get; set; }
        [GraphQLType(typeof(ChinesePhoneNumberType))]
        public string SmsCaptchaPhoneNumber { get; set; }
    }
}