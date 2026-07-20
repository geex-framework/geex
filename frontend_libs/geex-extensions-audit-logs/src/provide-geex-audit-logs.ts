import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createAuditLogsModule } from "./audit-logs.module";
import type { AuditLogsModule } from "./audit-logs.types";

export interface GeexAuditLogsOptions {
  readonly createAuditLogsModule?: (injector: Injector) => AuditLogsModule;
}

export const GEEX_AUDIT_LOGS_OPTIONS = new InjectionToken<Readonly<GeexAuditLogsOptions>>(
  "GEEX_AUDIT_LOGS_OPTIONS",
);

export function provideGeexAuditLogs(
  options: Readonly<GeexAuditLogsOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_AUDIT_LOGS_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        auditLogs: (options.createAuditLogsModule ?? createAuditLogsModule)(injector),
      }),
    }),
  ]);
}
