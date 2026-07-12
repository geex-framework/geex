using Geex.Extensions.Captcha.Abstractions;
using Geex.Gql.Types.Scalars;
using HotChocolate;
using HotChocolate.Types;
using MediatX;

namespace Geex.Extensions.Captcha.Abstractions.Requests;

public record SendCaptchaRequest : IRequest<Core.Captcha>
{
    public CaptchaProvider CaptchaProvider { get; set; } = default!;

    [GraphQLType(typeof(ChinesePhoneNumberType))]
    public string? SmsCaptchaPhoneNumber { get; set; }

    public class SendCaptchaRequestGqlConfig : GqlConfig.Input<SendCaptchaRequest>
    {
        protected override void Configure(IInputObjectTypeDescriptor<SendCaptchaRequest> descriptor)
        {
            descriptor.Validate(
                r => r.CaptchaProvider != CaptchaProvider.Sms || !r.SmsCaptchaPhoneNumber.IsNullOrEmpty(),
                "SmsCaptchaPhoneNumber is required for SMS captcha.");
        }
    }
}
