using Geex.Extensions.Captcha.Core.Entities;
using Geex.Gql.Types.Scalars;
using HotChocolate;
using MediatX;

namespace Geex.Extensions.Captcha.Abstractions.Requests;

public record SendCaptchaRequest : IRequest<Core.Entities.Captcha>
{
    public CaptchaProvider CaptchaProvider { get; set; }

    [GraphQLType(typeof(ChinesePhoneNumberType))]
    public string? SmsCaptchaPhoneNumber { get; set; }
}
