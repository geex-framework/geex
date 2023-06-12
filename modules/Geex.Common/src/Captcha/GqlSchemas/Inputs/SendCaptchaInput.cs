using Geex.Common.Captcha.Domain;
using Geex.Common.Gql.Types.Scalars;
using HotChocolate;

namespace Geex.Common.Captcha.GqlSchemas.Inputs
{
    public record SendCaptchaInput
    {
        public CaptchaProvider CaptchaProvider { get; set; }
        [GraphQLType(typeof(ChinesePhoneNumberType))]
        public string SmsCaptchaPhoneNumber { get; set; }
    }
}