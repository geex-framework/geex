import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import type { GeexModule } from "./modules";

export interface GeexModuleContributionContext {
  readonly injector: Injector;
  readonly modules: Readonly<Record<string, GeexModule>>;
}

export interface GeexModuleContribution {
  readonly createModules: (context: GeexModuleContributionContext) => Readonly<Record<string, GeexModule>>;
}

export const GEEX_MODULE_CONTRIBUTIONS = new InjectionToken<readonly GeexModuleContribution[]>(
  "GEEX_MODULE_CONTRIBUTIONS",
);

export function provideGeexModuleContribution(contribution: GeexModuleContribution): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: GEEX_MODULE_CONTRIBUTIONS,
      multi: true,
      useValue: contribution,
    },
  ]);
}
