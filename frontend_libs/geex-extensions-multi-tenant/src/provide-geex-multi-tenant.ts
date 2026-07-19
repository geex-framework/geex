import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createTenantModule } from "./tenant.module";
import type { TenantModule } from "./tenant.types";

export interface GeexMultiTenantOptions {
  readonly createTenantModule?: (injector: Injector) => TenantModule;
}

export const GEEX_MULTI_TENANT_OPTIONS = new InjectionToken<Readonly<GeexMultiTenantOptions>>(
  "GEEX_MULTI_TENANT_OPTIONS",
);

export function provideGeexMultiTenant(
  options: Readonly<GeexMultiTenantOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_MULTI_TENANT_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        tenant: (options.createTenantModule ?? createTenantModule)(injector),
      }),
    }),
  ]);
}
