import { GeexApproveStatus } from "@geexcode/geex-angular";

export const ApproveBadge: { [key: string]: "success" | "primary" | "warning" | "danger" } = {
  APPROVED: "success",
  SUBMITTED: "primary",
  DEFAULT: "warning",
};

export { GeexApproveStatus };
export { GEEX_APPROVE_STATUS_OPTIONS, DEFAULT_GEEX_APPROVE_STATUS_OPTIONS } from "@geexcode/geex-extensions-approval-flows";
export type { GeexApproveStatusOption } from "@geexcode/geex-extensions-approval-flows";
