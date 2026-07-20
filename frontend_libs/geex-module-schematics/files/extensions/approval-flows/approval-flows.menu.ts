import type { Menu } from "@delon/theme";

// ACL: schema has no ApprovalFlows_* permission enum yet.
export const menuContribution: Menu[] = [
  { text: "审批流", i18n: "ApprovalFlows.title", link: "/approval-flows", icon: "anticon-audit", children: [] },
];
