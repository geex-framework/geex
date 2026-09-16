import type { GeexModule } from "@geexcode/geex-angular";

export enum GeexApproveStatus {
  Default = "Default",
  Submitted = "Submitted",
  Approved = "Approved",
}

export type GeexApproveStatusOption = {
  readonly label: string;
  readonly value: GeexApproveStatus;
};

export const DEFAULT_GEEX_APPROVE_STATUS_OPTIONS: readonly GeexApproveStatusOption[] = [
  { label: "待上报", value: GeexApproveStatus.Default },
  { label: "已审批", value: GeexApproveStatus.Approved },
  { label: "已上报", value: GeexApproveStatus.Submitted },
];

export interface ApprovalFlowsModule extends GeexModule<{
  statusOptions: readonly GeexApproveStatusOption[];
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    approvalFlows: ApprovalFlowsModule;
  }
}
