import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createApprovalFlowsModule } from "./approval-flows.module";
import {
  DEFAULT_GEEX_APPROVE_STATUS_OPTIONS,
  type ApprovalFlowsModule,
  type GeexApproveStatusOption,
} from "./approval-flows.types";

export interface GeexApprovalFlowsOptions {
  readonly statusOptions?: readonly GeexApproveStatusOption[];
  readonly createApprovalFlowsModule?: (
    injector: Injector,
    statusOptions: readonly GeexApproveStatusOption[],
  ) => ApprovalFlowsModule;
}

export const GEEX_APPROVAL_FLOWS_OPTIONS = new InjectionToken<Readonly<GeexApprovalFlowsOptions>>(
  "GEEX_APPROVAL_FLOWS_OPTIONS",
);

export function provideGeexApprovalFlows(
  options: Readonly<GeexApprovalFlowsOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_APPROVAL_FLOWS_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => ({
        approvalFlows: (options.createApprovalFlowsModule ?? createApprovalFlowsModule)(
          injector,
          options.statusOptions ?? DEFAULT_GEEX_APPROVE_STATUS_OPTIONS,
        ),
      }),
    }),
  ]);
}
