import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createBackgroundJobModule } from "./background-job.module";
import type { BackgroundJobModule } from "./background-job.types";

export interface GeexBackgroundJobOptions {
  readonly createBackgroundJobModule?: (injector: Injector) => BackgroundJobModule;
}

export const GEEX_BACKGROUND_JOB_OPTIONS = new InjectionToken<Readonly<GeexBackgroundJobOptions>>(
  "GEEX_BACKGROUND_JOB_OPTIONS",
);

export function provideGeexBackgroundJob(
  options: Readonly<GeexBackgroundJobOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_BACKGROUND_JOB_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        backgroundJob: (options.createBackgroundJobModule ?? createBackgroundJobModule)(injector),
      }),
    }),
  ]);
}
