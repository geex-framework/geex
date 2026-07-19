import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  {
    children: [
      {
        text: "Settings参数",
        icon: "anticon-control",
        link: "/settings",
        acl: "settings_mutation_editSetting",
      },
    ],
    text: "系统设置",
    icon: "anticon anticon-tool",
    acl: "settings_query_settings",
  },
];
