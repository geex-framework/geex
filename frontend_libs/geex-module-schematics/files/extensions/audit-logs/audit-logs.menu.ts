import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  {
    text: "审计日志",
    i18n: "AuditLogs.title",
    icon: "anticon-file-search",
    link: "/audit-logs",
    acl: "AuditLogs_query_auditLogs",
  },
];
