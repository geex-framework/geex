import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { GEEX_MENU_CONTRIBUTIONS, provideGeexModuleContribution } from "@geexcode/geex-angular";
import { MockingMenuContribution } from "./mocking-menu.contribution";
import { createMockingModule } from "./mocking.module";
import type { MockingModule } from "./mocking.types";

export interface GeexMockingOptions {
  readonly createMockingModule?: (injector: Injector) => MockingModule;
  /** Register dynamic menu contribution (default true). */
  readonly menuContribution?: boolean;
}

export type GeexMockingProvideOptions = GeexMockingOptions;

export const GEEX_MOCKING_OPTIONS = new InjectionToken<Readonly<GeexMockingProvideOptions>>(
  "GEEX_MOCKING_OPTIONS",
);

export function provideGeexMocking(
  options: Readonly<GeexMockingProvideOptions> = {},
): EnvironmentProviders {
  const enableMenu = options.menuContribution !== false;
  return makeEnvironmentProviders([
    { provide: GEEX_MOCKING_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        mocking: (options.createMockingModule ?? createMockingModule)(injector),
      }),
    }),
    ...(enableMenu
      ? [
          MockingMenuContribution,
          { provide: GEEX_MENU_CONTRIBUTIONS, multi: true, useExisting: MockingMenuContribution },
        ]
      : []),
  ]);
}
