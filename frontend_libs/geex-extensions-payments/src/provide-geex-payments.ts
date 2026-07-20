import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createPaymentsModule } from "./payments.module";
import type { PaymentsModule } from "./payments.types";

export interface GeexPaymentsOptions {
  readonly createPaymentsModule?: (injector: Injector) => PaymentsModule;
}

export const GEEX_PAYMENTS_OPTIONS = new InjectionToken<Readonly<GeexPaymentsOptions>>(
  "GEEX_PAYMENTS_OPTIONS",
);

export function provideGeexPayments(
  options: Readonly<GeexPaymentsOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_PAYMENTS_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        payments: (options.createPaymentsModule ?? createPaymentsModule)(injector),
      }),
    }),
  ]);
}
