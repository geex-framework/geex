import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createBackupsModule } from "./backups.module";
import type { BackupsModule } from "./backups.types";

export interface GeexBackupsOptions {
  readonly createBackupsModule?: (injector: Injector) => BackupsModule;
}
export const GEEX_BACKUPS_OPTIONS = new InjectionToken<Readonly<GeexBackupsOptions>>("GEEX_BACKUPS_OPTIONS");

export function provideGeexBackups(options: Readonly<GeexBackupsOptions> = {}): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_BACKUPS_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({ backups: (options.createBackupsModule ?? createBackupsModule)(injector) }),
    }),
  ]);
}
