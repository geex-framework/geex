import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  {
    text: "租户配置",
    icon: "anticon-control",
    link: "/tenant",
    acl: "multiTenant_query_tenants",
  },
];
