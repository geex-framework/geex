namespace Geex.Extensions.Captcha.Abstractions;

public class CaptchaProvider : Enumeration<CaptchaProvider>
{
    public static CaptchaProvider Sms { get; } = FromValue(nameof(Sms));
    public static CaptchaProvider Image { get; } = FromValue(nameof(Image));
}
