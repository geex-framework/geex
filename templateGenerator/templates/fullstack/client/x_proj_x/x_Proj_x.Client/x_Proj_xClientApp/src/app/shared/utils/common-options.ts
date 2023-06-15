import { AuditStatus, SettingDefinition } from "../graphql/.generated/type";


// 审核状态
export const AuditStatusOptions = [
  {
    label: "待上报",
    value: AuditStatus.Default,
  },
  {
    label: "已审批",
    value: AuditStatus.Audited,
  },
  {
    label: "已上报",
    value: AuditStatus.Submitted,
  },
];

export const AuditBadge: { [key: string]: "success" | "primary" | "warning" | "danger" } = {
  AUDITED: "success",
  SUBMITTED: "primary",
  DEFAULT: "warning",
};
