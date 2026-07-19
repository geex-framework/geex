import type { GeexModule } from "@geexcode/geex-angular";

export enum GeexApproveStatus {
  DEFAULT = "DEFAULT",
  SUBMITTED = "SUBMITTED",
  APPROVED = "APPROVED",
}

export type GeexApproveStatusOption = {
  readonly label: string;
  readonly value: GeexApproveStatus;
};

export const DEFAULT_GEEX_APPROVE_STATUS_OPTIONS: readonly GeexApproveStatusOption[] = [
  { label: "待上报", value: GeexApproveStatus.DEFAULT },
  { label: "已审批", value: GeexApproveStatus.APPROVED },
  { label: "已上报", value: GeexApproveStatus.SUBMITTED },
];

export interface ApprovalFlowsModule extends GeexModule<{
  statusOptions: readonly GeexApproveStatusOption[];
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    approvalFlows: ApprovalFlowsModule;
  }
}
