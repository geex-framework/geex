import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from "@angular/core";
import { GEEX_APPROVE_STATUS_OPTIONS, GeexApproveStatusOption } from "./approve.tokens";

export interface GeexApprovalFlowsOptions {
  readonly statusOptions?: readonly GeexApproveStatusOption[];
}

export const GEEX_APPROVAL_FLOWS_OPTIONS =
  new InjectionToken<Readonly<GeexApprovalFlowsOptions>>("GEEX_APPROVAL_FLOWS_OPTIONS");

export function provideGeexApprovalFlows(
  options: Readonly<GeexApprovalFlowsOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_APPROVAL_FLOWS_OPTIONS, useValue: options },
    ...(options.statusOptions
      ? [{ provide: GEEX_APPROVE_STATUS_OPTIONS, useValue: options.statusOptions }]
      : []),
  ]);
}
