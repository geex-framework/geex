namespace Geex.Common.Captcha.Domain
{
    public class CaptchaProvider : Enumeration<CaptchaProvider>
    {
        public static CaptchaProvider Sms { get; } = FromValue(nameof(Sms));
        public static CaptchaProvider Image { get; } = FromValue(nameof(Image));
    }
}
