﻿using Geex.Common.Captcha.Domain;
using Geex.Gql.Types.Scalars;
using HotChocolate;

using MediatX;

namespace Geex.Common.Requests.Captcha
{
    public record SendCaptchaRequest : IRequest<Common.Captcha.Domain.Captcha>
    {
        public CaptchaProvider CaptchaProvider { get; set; }
        [GraphQLType(typeof(ChinesePhoneNumberType))]
        public string SmsCaptchaPhoneNumber { get; set; }
    }
}