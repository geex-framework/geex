import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createWechatAuthModule } from "./wechat-auth.module";
import type { GeexAuthenticationWechatOptions, WechatAuthModule } from "./wechat-auth.types";

export interface GeexAuthenticationWechatProvideOptions extends GeexAuthenticationWechatOptions {
  readonly createWechatAuthModule?: (
    injector: Injector,
    options: Readonly<GeexAuthenticationWechatOptions>,
  ) => WechatAuthModule;
}

export const GEEX_AUTHENTICATION_WECHAT_OPTIONS =
  new InjectionToken<Readonly<GeexAuthenticationWechatProvideOptions>>(
    "GEEX_AUTHENTICATION_WECHAT_OPTIONS",
  );

export function provideGeexAuthenticationWechat(
  options: Readonly<GeexAuthenticationWechatProvideOptions>,
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_AUTHENTICATION_WECHAT_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        wechatAuth: (options.createWechatAuthModule ?? createWechatAuthModule)(injector, options),
      }),
    }),
  ]);
}
