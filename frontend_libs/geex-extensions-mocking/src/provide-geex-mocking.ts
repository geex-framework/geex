import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createMockingModule } from "./mocking.module";
import type { MockingModule } from "./mocking.types";

export interface GeexMockingOptions {
  readonly createMockingModule?: (injector: Injector) => MockingModule;
}

export type GeexMockingProvideOptions = GeexMockingOptions;

export const GEEX_MOCKING_OPTIONS = new InjectionToken<Readonly<GeexMockingProvideOptions>>(
  "GEEX_MOCKING_OPTIONS",
);

export function provideGeexMocking(
  options: Readonly<GeexMockingProvideOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_MOCKING_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        mocking: (options.createMockingModule ?? createMockingModule)(injector),
      }),
    }),
  ]);
}
