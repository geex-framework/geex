using Geex.Extensions.Captcha.Abstractions;
using Geex.Gql.Types.Scalars;
using HotChocolate;
using MediatX;

namespace Geex.Extensions.Captcha.Abstractions.Requests;

public record SendCaptchaRequest : IRequest<Core.Captcha>
{
    public CaptchaProvider CaptchaProvider { get; set; } = default!;

    [GraphQLType(typeof(ChinesePhoneNumberType))]
    public string? SmsCaptchaPhoneNumber { get; set; }
}
