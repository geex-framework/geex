import {
  EnvironmentProviders,
  InjectionToken,
  Injector,
  importProvidersFrom,
  makeEnvironmentProviders,
} from "@angular/core";
import { GEEX_LOGIN_PATH, provideGeexModuleContribution } from "@geexcode/geex-angular";
import { OAuthModule, OAuthStorage } from "angular-oauth2-oidc";
import { createAuthenticationModule } from "./authentication.module";
import type { AuthenticationModule } from "./authentication.types";

export interface GeexAuthenticationOptions {
  readonly createAuthenticationModule?: (injector: Injector) => AuthenticationModule;
  readonly loginPath?: string;
  readonly oauthStorage?: () => OAuthStorage;
}

export const GEEX_AUTHENTICATION_OPTIONS = new InjectionToken<Readonly<GeexAuthenticationOptions>>(
  "GEEX_AUTHENTICATION_OPTIONS",
);

export function provideGeexAuthentication(
  options: Readonly<GeexAuthenticationOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_AUTHENTICATION_OPTIONS, useValue: options },
    ...(options.loginPath ? [{ provide: GEEX_LOGIN_PATH, useValue: options.loginPath }] : []),
    { provide: OAuthStorage, useFactory: options.oauthStorage ?? (() => localStorage) },
    importProvidersFrom(OAuthModule.forRoot()),
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        authentication: (options.createAuthenticationModule ?? createAuthenticationModule)(injector),
      }),
    }),
  ]);
}
