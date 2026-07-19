import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createSettingsModule } from "./settings.module";
import type { SettingsModule } from "./settings.types";

export interface GeexSettingsOptions {
  readonly createSettingsModule?: (injector: Injector) => SettingsModule;
}

export const GEEX_SETTINGS_OPTIONS = new InjectionToken<Readonly<GeexSettingsOptions>>(
  "GEEX_SETTINGS_OPTIONS",
);

export function provideGeexSettings(
  options: Readonly<GeexSettingsOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_SETTINGS_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        settings: (options.createSettingsModule ?? createSettingsModule)(injector),
      }),
    }),
  ]);
}
