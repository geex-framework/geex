import { InjectionToken } from "@angular/core";
import { GeexApproveStatus } from "@geexcode/geex-angular";

export type GeexApproveStatusOption = {
  readonly label: string;
  readonly value: GeexApproveStatus;
};

export const DEFAULT_GEEX_APPROVE_STATUS_OPTIONS: readonly GeexApproveStatusOption[] = [
  { label: "待上报", value: GeexApproveStatus.DEFAULT },
  { label: "已审批", value: GeexApproveStatus.APPROVED },
  { label: "已上报", value: GeexApproveStatus.SUBMITTED },
];

export const GEEX_APPROVE_STATUS_OPTIONS = new InjectionToken<readonly GeexApproveStatusOption[]>(
  "GEEX_APPROVE_STATUS_OPTIONS",
  { providedIn: "root", factory: () => DEFAULT_GEEX_APPROVE_STATUS_OPTIONS },
);
