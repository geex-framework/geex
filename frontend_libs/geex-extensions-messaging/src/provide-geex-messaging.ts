import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createMessagingModule } from "./messaging.module";
import type { MessagingModule } from "./messaging.types";

export interface GeexMessagingOptions {
  readonly createMessagingModule?: (
    injector: Injector,
    deps: () => { init: (force?: boolean) => Promise<unknown> } | undefined,
  ) => MessagingModule;
}

export const GEEX_MESSAGING_OPTIONS = new InjectionToken<Readonly<GeexMessagingOptions>>(
  "GEEX_MESSAGING_OPTIONS",
);

export function provideGeexMessaging(
  options: Readonly<GeexMessagingOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_MESSAGING_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector, modules }) => {
        const authentication = modules["authentication"];
        if (!authentication) {
          throw new Error(
            "provideGeexMessaging() requires provideGeexAuthentication() to be registered first",
          );
        }
        const messaging = (options.createMessagingModule ?? createMessagingModule)(injector, () => ({
          init: authentication.init.bind(authentication),
        }));
        return { messaging };
      },
    }),
  ]);
}
