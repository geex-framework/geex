import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createMultiTenantModule } from "./multi-tenant.module";
import type { MultiTenantModule } from "./multi-tenant.types";

export interface GeexMultiTenantOptions {
  readonly createMultiTenantModule?: (injector: Injector) => MultiTenantModule;
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
        multiTenant: (options.createMultiTenantModule ?? createMultiTenantModule)(injector),
      }),
    }),
  ]);
}
