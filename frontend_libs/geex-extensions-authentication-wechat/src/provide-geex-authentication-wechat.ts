import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createAuthenticationWechatModule } from "./authentication-wechat.module";
import type { AuthenticationWechatModule, GeexAuthenticationWechatOptions } from "./authentication-wechat.types";

export interface GeexAuthenticationWechatProvideOptions extends GeexAuthenticationWechatOptions {
  readonly createAuthenticationWechatModule?: (
    injector: Injector,
    options: Readonly<GeexAuthenticationWechatOptions>,
  ) => AuthenticationWechatModule;
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
        authenticationWechat: (options.createAuthenticationWechatModule ?? createAuthenticationWechatModule)(
          injector,
          options,
        ),
      }),
    }),
  ]);
}
