import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createCaptchaModule } from "./captcha.module";
import type { CaptchaModule } from "./captcha.types";

export interface GeexCaptchaOptions {
  readonly createCaptchaModule?: (injector: Injector) => CaptchaModule;
}

export const GEEX_CAPTCHA_OPTIONS = new InjectionToken<Readonly<GeexCaptchaOptions>>(
  "GEEX_CAPTCHA_OPTIONS",
);

export function provideGeexCaptcha(
  options: Readonly<GeexCaptchaOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_CAPTCHA_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        captcha: (options.createCaptchaModule ?? createCaptchaModule)(injector),
      }),
    }),
  ]);
}
