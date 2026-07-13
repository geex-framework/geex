namespace Geex.Extensions.Authentication.Wechat
{
    public class AuthenticationWechatModuleOptions : GeexModuleOptions
    {
        public override string BindSection => "AuthenticationModuleOptions:Wechat";

        public WechatAppCredentials? Web { get; set; }
        public WechatAppCredentials? MiniProgram { get; set; }
    }

    public class WechatAppCredentials
    {
        public string AppId { get; set; } = "";
        public string AppSecret { get; set; } = "";
    }
}
