import { geex } from "@geexcode/geex-angular";

export {
  GeexApproveStatus,
  DEFAULT_GEEX_APPROVE_STATUS_OPTIONS,
} from "@geexcode/geex-extensions-approval-flows";
export type { GeexApproveStatusOption } from "@geexcode/geex-extensions-approval-flows";

export function getApproveStatusOptions() {
  return geex.approvalFlows.statusOptions;
}
