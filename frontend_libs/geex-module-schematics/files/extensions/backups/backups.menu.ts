import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  { text: "备份管理", i18n: "Backups.title", link: "/backups", icon: "anticon-database", acl: "Backups_query_backups" },
];
