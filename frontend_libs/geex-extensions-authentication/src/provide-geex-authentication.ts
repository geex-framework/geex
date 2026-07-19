import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createAuthModule } from "./auth.module";
import type { AuthModule } from "./auth.types";

export interface GeexAuthenticationOptions {
  readonly createAuthModule?: (injector: Injector) => AuthModule;
}

export const GEEX_AUTHENTICATION_OPTIONS = new InjectionToken<Readonly<GeexAuthenticationOptions>>(
  "GEEX_AUTHENTICATION_OPTIONS",
);

export function provideGeexAuthentication(
  options: Readonly<GeexAuthenticationOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_AUTHENTICATION_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        auth: (options.createAuthModule ?? createAuthModule)(injector),
      }),
    }),
  ]);
}
