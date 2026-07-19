import type { Injector } from "@angular/core";
import {
  DEFAULT_GEEX_APPROVE_STATUS_OPTIONS,
  type ApprovalFlowsModule,
  type GeexApproveStatusOption,
} from "./approval-flows.types";

export function createApprovalFlowsModule(
  _injector: Injector,
  statusOptions: readonly GeexApproveStatusOption[] = DEFAULT_GEEX_APPROVE_STATUS_OPTIONS,
): ApprovalFlowsModule {
  return {
    statusOptions,
    init: async () => undefined,
  };
}
