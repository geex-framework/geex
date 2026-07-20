import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  {
    text: "系统设置",
    i18n: "Common.menu.settings",
    icon: "anticon-tool",
    link: "/settings",
    acl: "settings_query_settings",
  },
];
