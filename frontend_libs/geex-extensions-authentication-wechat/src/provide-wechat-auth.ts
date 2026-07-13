import { Provider } from "@angular/core";
import { WECHAT_AUTH_CONFIG, WechatWebLoginService } from "./wechat-web-login.service";
import { WechatAuthConfig } from "./types";

export function provideWechatAuth(config: WechatAuthConfig): Provider[] {
  return [
    { provide: WECHAT_AUTH_CONFIG, useValue: config },
    WechatWebLoginService,
  ];
}
