using Geex.Common.Abstractions;
using JetBrains.Annotations;

namespace Geex.Common.Captcha.Domain
{
    public class CaptchaProvider : Enumeration<CaptchaProvider>
    {
        public CaptchaProvider([NotNull] string name, string value) : base(name, value)
        {
        }

        public CaptchaProvider(string value) : base(value)
        {
        }

        public static CaptchaProvider Sms { get; } = new CaptchaProvider(nameof(Sms));
        public static CaptchaProvider Image { get; } = new CaptchaProvider(nameof(Image));
    }
}