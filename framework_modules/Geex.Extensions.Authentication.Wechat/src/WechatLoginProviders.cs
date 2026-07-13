namespace Geex.Extensions.Authentication.Wechat
{
    public class WechatLoginProviders : LoginProviderEnum
    {
        public static WechatLoginProviders WechatWeb { get; } =
            FromValue<WechatLoginProviders>(nameof(WechatWeb));

        public static WechatLoginProviders WechatMiniProgram { get; } =
            FromValue<WechatLoginProviders>(nameof(WechatMiniProgram));
    }
}
