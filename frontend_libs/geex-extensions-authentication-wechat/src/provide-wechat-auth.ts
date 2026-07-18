import { EnvironmentProviders, makeEnvironmentProviders } from "@angular/core";
import { GEEX_AUTHENTICATION_WECHAT_OPTIONS } from "./authentication-wechat.tokens";
import { GeexAuthenticationWechatOptions } from "./types";
import { WechatWebLoginService } from "./wechat-web-login.service";

export function provideGeexAuthenticationWechat(
  options: Readonly<GeexAuthenticationWechatOptions>,
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_AUTHENTICATION_WECHAT_OPTIONS, useValue: options },
    WechatWebLoginService,
  ]);
}

/** @deprecated Use `provideGeexAuthenticationWechat`. */
export const provideWechatAuth = provideGeexAuthenticationWechat;
